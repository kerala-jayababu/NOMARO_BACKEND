using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.Data.SqlClient;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using System.Data;

namespace YourNamespace.Services.Implementation
{
    public class SalaryGenerationService : ISalaryGenerationService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryGenerationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApprovalWorkflowService _approvalWorkflowService;

        public SalaryGenerationService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryGenerationService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _approvalWorkflowService = approvalWorkflowService;
        }

        public async Task<IEnumerable<SalaryGenerationDto>> GetSalaryConfigs(
        int employeeId,
        int? idSalaryMonth = null,
        string? dropdownFilter = null,
        int? idDepartment = null,
        int? idDesignation = null)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IdSalaryMonth", idSalaryMonth, DbType.Int32);
                    parameters.Add("@IdApprover", employeeId, DbType.Int32); // assuming employeeId is the approver

                    var result = await connection.QueryAsync<SalaryGenerationDto>(
                        "GetDataForSalaryApproval",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var filteredResult = result.AsQueryable();

                    // Apply dropdown filter
                    if (!string.IsNullOrEmpty(dropdownFilter) && !dropdownFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        var status = dropdownFilter.ToUpperInvariant();

                        filteredResult = status switch
                        {
                            "SUBMITTED" => filteredResult.Where(r => r.ApprovalStatus == "SUBMITTED"),
                            "HR APPROVED" => filteredResult.Where(r => r.ApprovalStatus == "HR Approved"),
                            "FM APPROVED" => filteredResult.Where(r => r.ApprovalStatus == "FM Approved"),
                            "APPROVED" => filteredResult.Where(r => r.ApprovalStatus == "APPROVED"),
                            "DRAFT GENERATED" => filteredResult.Where(r => r.ApprovalStatus == "DRAFT"),
                            "REJECTED" => filteredResult.Where(r => r.ApprovalStatus == "REJECTED"),
                            "NOT GENERATED" => filteredResult.Where(r => r.ApprovalStatus == "Not Generated"),
                            _ => filteredResult
                        };
                    }

                    // Apply Department filter
                    if (idDepartment.HasValue)
                    {
                        filteredResult = filteredResult.Where(r => r.IdDepartment == idDepartment.Value);
                    }

                    // Apply Designation filter
                    if (idDesignation.HasValue)
                    {
                        filteredResult = filteredResult.Where(r => r.IdDesignation == idDesignation.Value);
                    }

                    return filteredResult.OrderBy(r => r.EmployeeName).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary configurations via stored procedure.");
                throw new Exception("An error occurred while fetching configurations. Please try again later.");
            }
        }


        public async Task<IEnumerable<SalaryGenerationStatusDto>> GenerateSalaryDraft(string employeeIds, int idSalaryMonth, int idEmployeeCreated)
        {
            

            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var idEmployeesString = employeeIds;

                    var parameters = new DynamicParameters();
                    parameters.Add("@IdEmployeesString", idEmployeesString);
                    parameters.Add("@idSalaryMonth", idSalaryMonth);
                    parameters.Add("@IdEmployeeCreated", idEmployeeCreated);

                    var salaryStatusList = (await connection.QueryAsync<SalaryGenerationStatusDto>(
                 "GenerateSalary_MultipleEmployees",
                 parameters,
                 commandType: System.Data.CommandType.StoredProcedure)).ToList();

                    _logger.LogInformation($"Salary Status Count: {salaryStatusList.Count()}");

                    return salaryStatusList;
                
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating salary draft for employees.");
                throw new Exception("An error occurred while generating salary drafts. Please try again.");
            }
        }

        public async Task<int> UndoGeneratedDraftSalary(string employeeIds, int idSalaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }

            try
            {
                // Convert comma-separated string to a list of integers
                var employeeIdList = employeeIds.Split(',')
                                                .Select(id => int.Parse(id.Trim()))
                                                .ToList();

                // Find records to delete
                var recordsToDelete = await _dbContext.EmployeeSalaries
                    .Where(es => employeeIdList.Contains(es.IdEmployee) && es.IdSalaryMonth == idSalaryMonth)
                    .ToListAsync();

                var idemployeeSalaries= recordsToDelete.Select(x => x.IdEmployeeSalary).ToList();
                if (!recordsToDelete.Any())
                {
                    return 0; // No records found to delete
                }
                        var salaryDetailsToDelete = await _dbContext.EmployeeSalaryDetails
                .Where(x => idemployeeSalaries.Contains((int)x.IdEmployeeSalary))
                .ToListAsync();

                var bankRemittanceToDelete = await _dbContext.BankRemittance
              .Where(x => idemployeeSalaries.Contains((int)x.IdEmployeeSalary))
              .ToListAsync();


                _dbContext.EmployeeSalaryDetails.RemoveRange(salaryDetailsToDelete);

                // Remove EmployeeSalaries records
                _dbContext.EmployeeSalaries.RemoveRange(recordsToDelete);


                _dbContext.BankRemittance.RemoveRange(bankRemittanceToDelete);
                int deletedCount = await _dbContext.SaveChangesAsync();
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing generated draft salary.");
                throw new Exception("An error occurred while undoing draft salaries. Please try again later.");
            }
        }

        public async Task<bool> SubmitSalaryDetails(string employeeIds, int idSalaryMonth, int idEmployeeCreated)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var employeeIdList = employeeIds.Split(',').Select(int.Parse).ToList();

                var employeeSalaries = await _dbContext.EmployeeSalaries
                    .Where(es => employeeIdList.Contains(es.IdEmployee) && es.IdSalaryMonth == idSalaryMonth)
                    .ToListAsync();

                if (!employeeSalaries.Any())
                {
                    throw new Exception("No salary records found for the provided employee IDs and salary month.");
                }

                var currentDate = DateTime.Now;
                employeeSalaries.ForEach(salary =>
                {
                    salary.ApprovalStatus = "SUBMITTED";
                    salary.CreatedDate = currentDate;
                    salary.CreatedBy = idEmployeeCreated;
                    salary.ModifiedBy = idEmployeeCreated;
                    salary.ModifiedDate = currentDate;
                });

                _dbContext.EmployeeSalaries.UpdateRange(employeeSalaries);

                await _dbContext.SaveChangesAsync();

                var entityCode = _configuration["WorkflowEntityCodes:EMPSALGEN"];

                // Step: Call the approval workflow service
                foreach (var salary in employeeSalaries)
                {
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                        salary.IdEmployeeSalary,
                        entityCode,
                        idEmployeeCreated,
                        "SUBMITTED",
                        null
                    );

                    if (!result.Contains("Approval workflow initiated", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Failed to initiate approval workflow for Salary ID: {salary.IdEmployeeSalary}. Error: {result}");
                    }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting on salary generation.");
                throw new Exception("An error occurred while generating salary drafts. Please try again.");
            }
        }

        public async  Task<IEnumerable<SalaryGenerationDetailsDto>> GetSalaryGeneratedDetails(int? idSalaryMonth = null, string? dropdownFilter = null)
        {
            var query = new StringBuilder(@"
        SELECT 
            -- Employee Salary Information
            es.IdEmployeeSalary,
            es.IdEmployee,
            es.TotalEarnings,
            es.TotalDeductions,
            es.TaxAmountAccounted,
            es.ApprovalStatus,

            -- Employee Information
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            d.DesignationName,
            e.IdDepartment,
            dep.DepartmentName,
            e.JoiningDate,
            e.LastWorkingDay,	
            e.Gender,
            e.EmailID,
            e.PhoneNumber1,
            e.PhoneNumber2, 
            
            -- Salary Details
            esd.IdEmployeeSalaryDetail,
            esd.IdSalaryHead,
            esd.SalaryHeadName,
            esd.SalaryHeadType,
            COALESCE(esd.Amount, 0) AS Amount,
            COALESCE(esd.AmountInUSD, 0) AS AmountInUSD

        FROM EmployeeSalaries es
        LEFT JOIN Employees e ON es.IdEmployee = e.IdEmployee
        LEFT JOIN Departments dep ON e.IdDepartment = dep.IdDepartment
        LEFT JOIN Designations d ON e.IdDesignation = d.IdDesignation
        LEFT JOIN EmployeeSalaryDetails esd ON es.IdEmployeeSalary = esd.IdEmployeeSalary
        WHERE 1=1
    ");

            var parameters = new DynamicParameters();

            // Apply Salary Month filter
            if (idSalaryMonth.HasValue)
            {
                query.Append(" AND es.IdSalaryMonth = @IdSalaryMonth ");
                parameters.Add("IdSalaryMonth", idSalaryMonth);
            }

            // Apply Dropdown Filter for Approval Status
            if (!string.IsNullOrEmpty(dropdownFilter) && !dropdownFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query.Append(" AND es.ApprovalStatus = @ApprovalStatus ");
                parameters.Add("ApprovalStatus", dropdownFilter);
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var lookup = new Dictionary<int, SalaryGenerationDetailsDto>();

                    await connection.QueryAsync<SalaryGenerationDetailsDto, SalaryDetailDto, SalaryGenerationDetailsDto>(
                        query.ToString(),
                        (salary, detail) =>
                        {
                            if (!lookup.TryGetValue(salary.IdEmployeeSalary, out var salaryDto))
                            {
                                salaryDto = salary;
                                lookup.Add(salary.IdEmployeeSalary, salaryDto);
                            }

                            if (detail != null)
                            {
                                salaryDto.SalaryDetails.Add(detail);
                            }

                            return salaryDto;
                        },
                        parameters,
                        splitOn: "IdEmployeeSalaryDetail"
                    );

                    return lookup.Values;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary configurations.");
                throw new Exception("An error occurred while fetching configurations. Please try again later.");
            }
        }

        public async Task<dynamic> ExportSalaryGenerationDetails(string employeeIds, int idSalaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)               
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IdSalaryMonth", idSalaryMonth, DbType.Int32);
                    parameters.Add("@IdEmployeesString", employeeIds, DbType.String);

                    var result = await connection.QueryAsync<dynamic>(
                        "ExportSalaryGenerationDetails",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting salary generation details.");
                throw new Exception("An error occurred while exporting salary details. Please try again.");
            }
        }


        public async Task<dynamic> GetSalaryapprovalValue(int employeeId)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    await connection.OpenAsync();

                    string sql = @"Select idsalarymonth from EmployeeSalaries where IdEmployeeSalary = (Select max(idemployeesalary) from EmployeeSalaries)";

                    //var parameters = new { EmployeeIdParam = employeeId };
                    var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql);

                    if (result == null)
                        return null;

                    int? idDesignation = await _dbContext.Employees.Where(x=>x.IdEmployee == employeeId).Select(x=>x.IdDesignation).FirstOrDefaultAsync();
                    int maxSalaryMonth = result.idsalarymonth;

                    // Fetch WorkflowConfig Id
                    var workflowConfigId = await _dbContext.WorkFlowConfig
                        .Where(x => x.EntityCode == "EMPSALGEN")
                        .Select(x => x.IdWorkFlowConfig)
                        .FirstOrDefaultAsync();

                    // Get current level
                    var currentDetail = await _dbContext.WorkFlowConfigDetails
                        .FirstOrDefaultAsync(x => x.IdWorkFlowConfig == workflowConfigId && x.ApprovalAuthorityID == idDesignation);

                    string approvalStatus;

                    if (currentDetail != null)
                    {
                        int levelNumber = currentDetail.LevelNumber - 1;

                        if (levelNumber == 0)
                        {
                            approvalStatus = "Submitted";
                        }
                        else
                        {
                            approvalStatus = await _dbContext.WorkFlowConfigDetails
                                .Where(x => x.IdWorkFlowConfig == workflowConfigId && x.LevelNumber == levelNumber)
                                .Select(x => x.ApprovalStatusName)
                                .FirstOrDefaultAsync();
                        }
                    }
                    else
                    {
                        approvalStatus = "ALL";
                    }

                    return new
                    {
                        MaxSalaryMonth = maxSalaryMonth,
                        ApprovalStatus = approvalStatus
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salary approval value.");
                throw new Exception("An error occurred while retrieving salary approval. Please try again.");
            }
        }








        public async Task<dynamic> ExportSalaryGenerationDetailsForApproved(string employeeIds, int idSalaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IdSalaryMonth", idSalaryMonth, DbType.Int32);
                    parameters.Add("@IdEmployeesString", employeeIds, DbType.String);

                    var result = await connection.QueryAsync<dynamic>(
                        "ExportSalaryGenerationDetails_ForApproval",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting salary generation details.");
                throw new Exception("An error occurred while exporting salary details. Please try again.");
            }
        }
        public async Task<bool> CheckcurrencyConversions()
        {
            try
            {
                var connStr = _configuration.GetConnectionString("DbContext");
                using (var connection = new SqlConnection(connStr))
                {
                    var query = @"SELECT CASE WHEN EXISTS (
                            SELECT 1 
                            FROM [UAT_GIAGY].[dbo].[CurrencyConversions]
                            WHERE FromCurrency = 'USD'
                            AND ToCurrency = 'GYD'
                            AND RateDate >= DATEADD(DAY, -5, GETDATE())
                          ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END";

                    await connection.OpenAsync();
                    var result = await connection.ExecuteScalarAsync<bool>(query);
                    return result;
                }

                //using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                //{
                //    var query = @"
                //SELECT CASE 
                //    WHEN EXISTS (
                //        SELECT 1 
                //        FROM [UAT_GIAGY].[dbo].[CurrencyConversions]
                //        WHERE FromCurrency = 'USD'
                //          AND ToCurrency = 'GYD'
                //          AND RateDate >= DATEADD(DAY, -5, GETDATE())
                //    )
                //    THEN CAST(1 AS BIT)
                //    ELSE CAST(0 AS BIT)
                //END";

                //    var result = await connection.ExecuteScalarAsync<bool>(query);
                //    await connection.CloseAsync(); // ✅ Explicit close
                //    return result;
                  
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking currency conversion rates.");
                return false;
            }
        }




        public async Task<List<SalaryUploadResponseDto>> UploadSalaryDetails(UploadSalaryGenerationDetailsDto uploadSalaryGenerationDetails,int EmployeeID)
        {
            try
            {

                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@IdSalaryMonth", uploadSalaryGenerationDetails.IdSalaryMonth, DbType.Int32);
                    parameters.Add("@CreatedBy", EmployeeID, DbType.Int32); // Change this as per your authentication logic
                    parameters.Add("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(uploadSalaryGenerationDetails.EmployeeSalaryJsonData), DbType.String);

                    var result = await connection.QueryAsync<SalaryUploadResponseDto>(
                        "dbo.Upload_SalaryDetailsFromJSON",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var employeeList = await connection.QueryAsync<Employee>(
                "SELECT IdEmployee, EmployeeCode FROM Employees"
            );

                    // Convert to dictionary for quick lookup
                    var employeeDict = employeeList.ToDictionary(e => e.IdEmployee, e => e.EmployeeCode);

                    // Append EmployeeCode to result
                    foreach (var item in result)
                    {
                        if (employeeDict.ContainsKey(item.IdEmployee))
                        {
                            item.EmployeeCode = employeeDict[item.IdEmployee];
                        }
                    }

                    return result.ToList();                   
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading  salary generation details.");
                throw new Exception("An error occurred while uploading salary details. Please try again.");
            }
        }

        public async Task<List<EmployeePayslipDto>> GeneratePayslipPdf(string idEmployeeSalary)
        {
            if (string.IsNullOrWhiteSpace(idEmployeeSalary))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }
            var  systemparamters = await _dbContext.SystemParameters.ToListAsync();

            try
            {
                var idEmployeeSalaryliST = idEmployeeSalary.Split(',').Select(id => id.Trim()).ToList();

                // Fetch Salary Details for Employees
                var salaries = await _dbContext.EmployeeSalaries
                         .Where(s => idEmployeeSalaryliST.Contains(s.IdEmployeeSalary.ToString()))
                         .ToListAsync();

                var employeeIdList = salaries.Select(x => x.IdEmployee).ToList();

                // Fetch Employee Details
                var employees = await _dbContext.Employees
       .Where(e => employeeIdList.Contains(e.IdEmployee))
       .Join(_dbContext.Designations,
           emp => emp.IdDesignation,
           des => des.IdDesignation,
           (emp, des) => new { emp, des })
       .Join(_dbContext.Departments,
           combined => combined.emp.IdDepartment,
           dept => dept.IdDepartment,
           (combined, dept) => new
           {
               combined.emp.IdEmployee,
               combined.emp.EmployeeCode,
               FirstName = combined.emp.FirstName + " " + combined.emp.MiddleName+" " + combined.emp.LastName,
               Position = combined.des.DesignationName, 
               Department = dept.DepartmentName         
           })
       .ToListAsync();


                if (!employees.Any())
                {
                    throw new Exception("No employees found for the given IDs.");
                }

               

                if (!salaries.Any())
                {
                    throw new Exception("No salary details found for the given employees and salary month.");
                }

                // Fetch Salary Breakdown (Earnings & Deductions) for Employees
                var salaryIds = salaries.Select(s => s.IdEmployeeSalary).ToList();
                var salaryDetails = await _dbContext.EmployeeSalaryDetails
                    .Where(sd => salaryIds.Contains((int)sd.IdEmployeeSalary))
                    .ToListAsync();

                var payslips = new List<EmployeePayslipDto>();

                foreach (var salary in salaries)
                {
                    var employee = employees.FirstOrDefault(e => e.IdEmployee == salary.IdEmployee);
                    if (employee == null) continue;

                    var employeeSalaryDetails = salaryDetails
                        .Where(sd => sd.IdEmployeeSalary == salary.IdEmployeeSalary)
                        .ToList();

                    // Grouping Salary Details into Earnings and Deductions
                    var earnings = employeeSalaryDetails
                        .Where(sd => sd.SalaryHeadType == "EARNING")
                        .Select(sd => new EmployeeSalaryDetailsDto
                        {
                            Description = sd.SalaryHeadName,
                            AmountG = sd.Amount ?? 0,
                            AmountUS = sd.AmountInUSD ?? 0,
                            YTDAmountUSD =sd.YTDAmountUSD??0,
                            YTDAmountG = sd.YTDAmount ?? 0 // Set to 0 as per requirement
                        })
                        .ToList();

                    var deductions = employeeSalaryDetails
                        .Where(sd => sd.SalaryHeadType == "DEDUCTION")
                        .Select(sd => new EmployeeSalaryDetailsDto
                        {
                            Description = sd.SalaryHeadName,
                            AmountG = sd.Amount ?? 0,
                            AmountUS = sd.AmountInUSD ?? 0,
                            YTDAmountUSD = sd.YTDAmountUSD ?? 0,
                            YTDAmountG = sd.YTDAmount ?? 0 
                        })
                        .ToList();

                    // Construct Employee Payslip DTO
                    var payslip = new EmployeePayslipDto
                    {
                        IdEmployeeSalary = salary.IdEmployeeSalary,
                        EmployeeCode = employee.EmployeeCode,
                        EmployeeName = employee.FirstName,
                        Position = employee.Position,
                        Department = employee.Department,
                        Period = salary.SalaryMonthText,
                        PayslipGeneratedDate = salary.GeneratedDate.ToString("yyyy-MM-dd"),
                        Earnings = earnings,
                        Deductions = deductions,
                        logo = systemparamters.Where(x=>x.ParameterName == "CompanyLogo").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                        logoType = systemparamters.Where(x=>x.ParameterName == "CompanyLogo").Select(x => x.DataType).FirstOrDefault(),
                        stamp = systemparamters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                        stampType = systemparamters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.DataType).FirstOrDefault(),
                    };

                    payslips.Add(payslip);
                }

                return payslips;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslips.");
                throw new Exception("An error occurred while generating the payslips.");
            }
        }


        public async Task<List<EmployeePayslipDto>> GenerateNotificationForEmployeeSalary(string idEmployeeSalary, int? IdSalaryMonth)
        {
           
            var systemparamters = await _dbContext.SystemParameters.ToListAsync();

            try
            {
                List<EmployeeSalaries> salaries;
                if (!string.IsNullOrWhiteSpace(idEmployeeSalary))
                {
                    var idList = idEmployeeSalary.Split(',')
                        .Select(x => x.Trim()).ToList();

                    salaries = await _dbContext.EmployeeSalaries
                        .Where(s => idList.Contains(s.IdEmployeeSalary.ToString()) && s.ApprovalStatus== "APPROVED")
                        .ToListAsync();
                }
                else
                {
                    // we know idSalaryMonth.HasValue == true
                    salaries = await _dbContext.EmployeeSalaries
                        .Where(s => s.IdSalaryMonth == IdSalaryMonth.Value && s.ApprovalStatus == "APPROVED")
                        .ToListAsync();
                }

               // var idEmployeeSalaryliST = idEmployeeSalary.Split(',').Select(id => id.Trim()).ToList();

               

                var employeeIdList = salaries.Select(x => x.IdEmployee).ToList();

                // Fetch Employee Details
                var employees = await _dbContext.Employees
       .Where(e => employeeIdList.Contains(e.IdEmployee))
       .Join(_dbContext.Designations,
           emp => emp.IdDesignation,
           des => des.IdDesignation,
           (emp, des) => new { emp, des })
       .Join(_dbContext.Departments,
           combined => combined.emp.IdDepartment,
           dept => dept.IdDepartment,
           (combined, dept) => new
           {
               combined.emp.IdEmployee,
               combined.emp.EmployeeCode,
               FirstName = combined.emp.FirstName + " " + combined.emp.MiddleName + " " + combined.emp.LastName,
               Position = combined.des.DesignationName,
               EmailId=combined.emp.EmailID,
               Department = dept.DepartmentName
           })
       .ToListAsync();


                if (!employees.Any())
                {
                    throw new Exception("No employees found for the given IDs.");
                }



                if (!salaries.Any())
                {
                    throw new Exception("No salary details found for the given employees and salary month.");
                }

                // Fetch Salary Breakdown (Earnings & Deductions) for Employees
                var salaryIds = salaries.Select(s => s.IdEmployeeSalary).ToList();
                var salaryDetails = await _dbContext.EmployeeSalaryDetails
                    .Where(sd => salaryIds.Contains((int)sd.IdEmployeeSalary))
                    .ToListAsync();

                var payslips = new List<EmployeePayslipDto>();
                var remittances = await _dbContext.BankRemittance
    .Where(r => salaryIds.Contains((int)r.IdEmployeeSalary))
    .ToListAsync();

                foreach (var salary in salaries)
                {
                    var employee = employees.FirstOrDefault(e => e.IdEmployee == salary.IdEmployee);
                    if (employee == null) continue;

                    var employeeSalaryDetails = salaryDetails
                        .Where(sd => sd.IdEmployeeSalary == salary.IdEmployeeSalary)
                        .ToList();

                    // Grouping Salary Details into Earnings and Deductions
                    var earnings = employeeSalaryDetails
                        .Where(sd => sd.SalaryHeadType == "EARNING")
                        .Select(sd => new EmployeeSalaryDetailsDto
                        {
                            Description = sd.SalaryHeadName,
                            AmountG = sd.Amount ?? 0,
                            AmountUS = sd.AmountInUSD ?? 0,
                            YTDAmountUSD = sd.YTDAmountUSD ?? 0,
                            YTDAmountG = sd.YTDAmount ?? 0 // Set to 0 as per requirement
                        })
                        .ToList();

                    var deductions = employeeSalaryDetails
                        .Where(sd => sd.SalaryHeadType == "DEDUCTION")
                        .Select(sd => new EmployeeSalaryDetailsDto
                        {
                            Description = sd.SalaryHeadName,
                            AmountG = sd.Amount ?? 0,
                            AmountUS = sd.AmountInUSD ?? 0,
                            YTDAmountUSD = sd.YTDAmountUSD ?? 0,
                            YTDAmountG = sd.YTDAmount ?? 0
                        })
                        .ToList();

                    // Construct Employee Payslip DTO
                    var payslip = new EmployeePayslipDto
                    {
                        EmployeeCode = employee.EmployeeCode,
                        EmployeeName = employee.FirstName,
                        Position = employee.Position,
                        EmailID = employee.EmailId,
                        Department = employee.Department,
                        Period = salary.SalaryMonthText,
                        PayslipGeneratedDate = salary.GeneratedDate.ToString("yyyy-MM-dd"),
                        Earnings = earnings,
                        Deductions = deductions,
                        logo = systemparamters.Where(x => x.ParameterName == "CompanyLogo").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                        logoType = systemparamters.Where(x => x.ParameterName == "CompanyLogo").Select(x => x.DataType).FirstOrDefault(),
                        stamp = systemparamters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                        stampType = systemparamters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.DataType).FirstOrDefault(),
                    };
                    payslip.BankRemittance = remittances
           .Where(r => r.IdEmployeeSalary == salary.IdEmployeeSalary)
           .Select(r => new BankRemittanceDto
           {
               IdBankRemittance = r.IdBankRemittance,
               BankName = r.BankName,
               AccountNumber = r.AccountNumber,
               ABARoutingNumber = r.ABARoutingNumber,
               AmountGTD = r.AmountGYD,
               AmountUSD = r.AmountUSD,
               DistributedPercent = r.DistributedPercentage,
               Currency = r.Currency,
               
           })
           .ToList();

                    payslips.Add(payslip);
                }

                return payslips;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslips.");
                throw new Exception("An error occurred while generating the payslips.");
            }
        }

        public async Task MarkSalaryEmailInProcessAsync(int idEmployeeSalary)
        {
            var salaryEntity = await _dbContext.EmployeeSalaries
                .FirstOrDefaultAsync(s => s.IdEmployeeSalary == idEmployeeSalary);

            if (salaryEntity != null)
            {
                salaryEntity.EmailStatus = "InProcess";
                salaryEntity.EmailSentDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning(
                    "Cannot mark EmployeeSalary {SalaryId} as InProcess—record not found.",
                    idEmployeeSalary);
            }
        }

        public async Task MarkSalaryEmailSentAsync(int idEmployeeSalary)
        {
            var salaryEntity = await _dbContext.EmployeeSalaries
                .FirstOrDefaultAsync(s => s.IdEmployeeSalary == idEmployeeSalary);

            if (salaryEntity != null)
            {
                salaryEntity.EmailStatus = "Sent";
                salaryEntity.EmailSentDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning(
                    "Cannot mark EmployeeSalary {SalaryId} as Sent—record not found.",
                    idEmployeeSalary);
            }
        }




        public async Task<IEnumerable<SalarySlipDto>> GetSalarySlips(int idSalaryMonthFrom, int idSalaryMonthTo, string? dropdownFilter = null)
        {
            var query = new StringBuilder(@"
    SELECT 
        es.IdEmployeeSalary,
        es.IdSalaryMonth,
        es.IdEmployee,
        sm.SalaryMonthText,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        dept.DepartmentName,
        e.JoiningDate,
        e.LastWorkingDay,
        e.Gender,
        e.EmailID,
        e.PhoneNumber1,
        e.PhoneNumber2,
        es.TotalEarnings,
        es.TotalDeductions,
        es.TaxableIncome,
        es.TaxAmountAccounted,
        es.EmailStatus,
        es.EmailSentDate,
        es.TaxAmountDeducted,
        es.ApprovalStatus
    FROM EmployeeSalaries es
    INNER JOIN Employees e ON es.IdEmployee = e.IdEmployee
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    INNER JOIN Departments dept ON e.IdDepartment = dept.IdDepartment
    INNER JOIN SalaryMonths sm ON es.IdSalaryMonth = sm.IdSalaryMonth
    WHERE es.IdSalaryMonth BETWEEN @IdSalaryMonthFrom AND @IdSalaryMonthTo and es.ApprovalStatus='APPROVED'
    ");

            var parameters = new DynamicParameters();
            parameters.Add("IdSalaryMonthFrom", idSalaryMonthFrom);
            parameters.Add("IdSalaryMonthTo", idSalaryMonthTo);

            // Apply dropdown filter if provided
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                query.Append(@" AND (
            e.EmployeeCode LIKE @DropdownFilter OR
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @DropdownFilter OR
            des.DesignationName LIKE @DropdownFilter OR
            dept.DepartmentName LIKE @DropdownFilter
        )");
                parameters.Add("DropdownFilter", $"%{dropdownFilter}%");
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var salarySlips = await connection.QueryAsync<SalarySlipDto>(query.ToString(), parameters);
                    return salarySlips;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Salary Slips.");
                throw new Exception("An error occurred while fetching salary slips. Please try again later.");
            }
        }


        
    }
}
