using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourNamespace.Services.Implementation
{
    public class SalaryGenerationService : ISalaryGenerationService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryGenerationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IAuditService _auditService;

        public SalaryGenerationService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryGenerationService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _approvalWorkflowService = approvalWorkflowService;
            _auditService = auditService;
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


        public async Task<IEnumerable<SalaryGenerationStatusDto>> GenerateSalaryDraft(int idSalaryMonth, int idEmployeeCreated)
        {
            

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@idSalaryMonth", idSalaryMonth);
                    parameters.Add("@IdEmployeeCreated", idEmployeeCreated);

                    var salaryStatusList = (await connection.QueryAsync<SalaryGenerationStatusDto>(
                 "GenerateMonthlySalary",
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

                // Find EmployeeSalaries records to delete
                var recordsToDelete = await _dbContext.EmployeeSalaries
                    .Where(es => employeeIdList.Contains(es.IdEmployee) && es.IdSalaryMonth == idSalaryMonth)
                    .ToListAsync();

                if (!recordsToDelete.Any())
                {
                    return 0; // No records found to delete
                }

                var idEmployeeSalaries = recordsToDelete.Select(x => x.IdEmployeeSalary).ToList();

                // Get related EmployeeSalaryDetails
                var salaryDetailsToDelete = await _dbContext.EmployeeSalaryDetails
                    .Where(x => idEmployeeSalaries.Contains((int)x.IdEmployeeSalary))
                    .ToListAsync();

                // Get related BankRemittance
                var bankRemittanceToDelete = await _dbContext.BankRemittance
                    .Where(x => idEmployeeSalaries.Contains((int)x.IdEmployeeSalary))
                    .ToListAsync();

                // ✅ Reset OvertimeTransactions (set IdSalaryMonthAccounted = null)
                var overtimeTransactions = await _dbContext.OvertimeTransactions
                    .Where(ot => employeeIdList.Contains(ot.IdEmployee)
                              && ot.IdSalaryMonthAccounted == idSalaryMonth)
                    .ToListAsync();

                foreach (var ot in overtimeTransactions)
                {
                    ot.IdSalaryMonthAccounted = null;
                }

                // Remove dependent records
                _dbContext.EmployeeSalaryDetails.RemoveRange(salaryDetailsToDelete);
                _dbContext.BankRemittance.RemoveRange(bankRemittanceToDelete);

                // Remove EmployeeSalaries
                _dbContext.EmployeeSalaries.RemoveRange(recordsToDelete);

                // Save all changes in one transaction
                int affectedRows = await _dbContext.SaveChangesAsync();

                return affectedRows;
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

                var beforeUpdate = employeeSalaries.Select(s => new
                {
                    s.IdEmployeeSalary,
                    s.IdEmployee,
                    s.IdSalaryMonth,
                    s.ApprovalStatus,
                    s.ModifiedBy,
                    s.ModifiedDate,
                    s.CreatedDate,
                    s.CreatedBy
                }).ToList();

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

                int count = 1;
                // Step: Call the approval workflow service
                foreach (var salary in employeeSalaries)
                {
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                        salary.IdEmployeeSalary,
                        entityCode,
                        idEmployeeCreated,
                        "SUBMITTED",
                        null,
                        null,
                        count
                    );
                    count++;
                    if (!result.Contains("Approval workflow initiated", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Failed to initiate approval workflow for Salary ID: {salary.IdEmployeeSalary}. Error: {result}");
                    }
                }

                await transaction.CommitAsync();
                await _auditService.LogAuditAsync("Update", "EmployeeSalary", idSalaryMonth, new { before = beforeUpdate, after = new { employeeIds = employeeIdList, salaryMonth = idSalaryMonth, updatedBy = idEmployeeCreated } });
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
                        .Where(x => x.EntityCode == "SALARYGEN")
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
                            FROM [CurrencyConversions]
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
                   .Where(e => employeeIdList.Contains((int)e.IdEmployee))
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
                        LogoHeightInPayslip =
                        float.TryParse(
                            systemparamters.Where(x => x.ParameterName == "LogoHeightInPayslip")
                                           .Select(x => x.ParameterValue)
                                           .FirstOrDefault(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var lh) && lh > 0 ? lh : 50f,

                              StampHeightInPayslip =
                        float.TryParse(
                            systemparamters.Where(x => x.ParameterName == "StampHeightInPayslip")
                                           .Select(x => x.ParameterValue)
                                           .FirstOrDefault(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var sh) && sh > 0 ? sh : 75f
                                        };

                                        payslips.Add(payslip);
                }

                await PopulatePayslipPrintDetails(payslips);
                return payslips;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslips.");
                throw new Exception("An error occurred while generating the payslips.");
            }
        }


        public async Task<EmployeePayslipDto> GetPayslipObjectAsync(int idEmployeeSalary)
        {
            if (idEmployeeSalary <= 0)
                throw new ArgumentException("Employee Salary ID cannot be empty.");

            var systemparamters = await _dbContext.SystemParameters.ToListAsync();

            var salary = await _dbContext.EmployeeSalaries
                         .FirstOrDefaultAsync(s => s.IdEmployeeSalary == idEmployeeSalary);

            if (salary == null)
                throw new Exception("No salary details found for the given ID.");

            var employee = await _dbContext.Employees
                .Where(e => e.IdEmployee == salary.IdEmployee)
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
                        FullName = combined.emp.FirstName + " " + combined.emp.MiddleName + " " + combined.emp.LastName,
                        Position = combined.des.DesignationName,
                        Department = dept.DepartmentName
                    })
                .FirstOrDefaultAsync();

            if (employee == null)
                throw new Exception("No employee found for the given salary ID.");

            var salaryDetails = await _dbContext.EmployeeSalaryDetails
                                  .Where(sd => sd.IdEmployeeSalary == idEmployeeSalary)
                                  .ToListAsync();
        
            var earnings = salaryDetails
                .Where(sd => sd.SalaryHeadType == "EARNING")
                .Select(sd => new EmployeeSalaryDetailsDto
                {
                    Description = sd.SalaryHeadName,
                    AmountG = sd.Amount ?? 0,
                    AmountUS = sd.AmountInUSD ?? 0,
                    YTDAmountUSD = sd.YTDAmountUSD ?? 0,
                    YTDAmountG = sd.YTDAmount ?? 0
                })
                .ToList();

            var deductions = salaryDetails
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

            var remittances = await _dbContext.BankRemittance
        .Where(r => r.IdEmployeeSalary == idEmployeeSalary)
        .Select(r => new BankRemittanceDto
        {
            IdBankRemittance = r.IdBankRemittance,
            BankName = r.BankName,
            AccountNumber = r.AccountNumber,
            ABARoutingNumber = r.ABARoutingNumber,
            AmountGTD = r.AmountGYD,
            AmountUSD = r.AmountUSD,
            DistributedPercent = r.DistributedPercentage,
            Currency = r.Currency
        })
        .ToListAsync();

            return new EmployeePayslipDto
            {
                IdEmployeeSalary = salary.IdEmployeeSalary,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.FullName,
                Position = employee.Position,
                Department = employee.Department,
                Period = salary.SalaryMonthText,
                PayslipGeneratedDate = salary.GeneratedDate.ToString("yyyy-MM-dd"),
                Earnings = earnings,
                Deductions = deductions,
                BankRemittance = remittances,
                logo = systemparamters.Where(x => x.ParameterName == "CompanyLogo")
                                      .Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                logoType = systemparamters.Where(x => x.ParameterName == "CompanyLogo")
                                          .Select(x => x.DataType).FirstOrDefault(),
                stamp = systemparamters.Where(x => x.ParameterName == "CompanySeal")
                                       .Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                stampType = systemparamters.Where(x => x.ParameterName == "CompanySeal")
                                           .Select(x => x.DataType).FirstOrDefault()
            };
        }


        public async Task<EmployeePayslipDto> GetPayslipDetailsForLeavePassage(int IdEmployee)
        {
            try
            {
                var systemParameters = await _dbContext.SystemParameters.ToListAsync();

                // Get latest salary record for the employee
                var latestSalary = await _dbContext.EmployeeSalaries
                    .Where(s => s.IdEmployee == IdEmployee && s.ApprovalStatus== "APPROVED")
                    .OrderByDescending(s => s.IdEmployeeSalary)
                    .FirstOrDefaultAsync();

                if (latestSalary == null)
                {
                    // No salary slip found for the employee
                    return null;
                }

                // Get employee details (including department, designation)
                var employee = await _dbContext.Employees
                    .Where(e => e.IdEmployee == IdEmployee)
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
                            Department = dept.DepartmentName
                        })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    // No employee details found
                    return null;
                }

                // Get salary breakdown (earnings & deductions)
                var salaryDetails = await _dbContext.EmployeeSalaryDetails
                    .Where(sd => sd.IdEmployeeSalary == latestSalary.IdEmployeeSalary)
                    .ToListAsync();

                var earnings = salaryDetails
                    .Where(sd => sd.SalaryHeadType == "EARNING")
                    .Select(sd => new EmployeeSalaryDetailsDto
                    {
                        Description = sd.SalaryHeadName,
                        AmountG = sd.Amount ?? 0,
                        AmountUS = sd.AmountInUSD ?? 0,
                        YTDAmountUSD = sd.YTDAmountUSD ?? 0,
                        YTDAmountG = sd.YTDAmount ?? 0
                    })
                    .ToList();

                var deductions = salaryDetails
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

                // Construct and return the payslip DTO
                var payslip = new EmployeePayslipDto
                {
                    IdEmployeeSalary = latestSalary.IdEmployeeSalary,
                    EmployeeCode = employee.EmployeeCode,
                    EmployeeName = employee.FirstName,
                    Position = employee.Position,
                    Department = employee.Department,
                    Period = latestSalary.SalaryMonthText,
                    PayslipGeneratedDate = latestSalary.GeneratedDate.ToString("yyyy-MM-dd"),
                    Earnings = earnings,
                    Deductions = deductions,
                    logo = systemParameters.Where(x => x.ParameterName == "CompanyLogo").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                    logoType = systemParameters.Where(x => x.ParameterName == "CompanyLogo").Select(x => x.DataType).FirstOrDefault(),
                    stamp = systemParameters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.ParameterBinaryValue).FirstOrDefault(),
                    stampType = systemParameters.Where(x => x.ParameterName == "CompanySeal").Select(x => x.DataType).FirstOrDefault(),
                };

                return payslip;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payslip for Leave Passage.");
                throw new Exception("An error occurred while retrieving the payslip.");
            }
        }

        public async Task<List<EmployeePayslipDto>> GenerateNotificationForEmployeeSalary(string idEmployeeSalary)
        {
           
            var systemparamters = await _dbContext.SystemParameters.ToListAsync();

            try
            {
                List<EmployeeSalaries> salaries = new List<EmployeeSalaries>();
                if (!string.IsNullOrWhiteSpace(idEmployeeSalary))
                {
                    var idList = idEmployeeSalary.Split(',')
                        .Select(x => x.Trim()).ToList();

                    salaries = await _dbContext.EmployeeSalaries
                        .Where(s => idList.Contains(s.IdEmployeeSalary.ToString()) && s.ApprovalStatus== "APPROVED")
                        .ToListAsync();
                }
                //else
                //{
                //    // we know idSalaryMonth.HasValue == true
                //    salaries = await _dbContext.EmployeeSalaries
                //        .Where(s => s.IdSalaryMonth == IdSalaryMonth.Value && s.ApprovalStatus == "APPROVED")
                //        .ToListAsync();
                //}

                //var idEmployeeSalaryliST = idEmployeeSalary.Split(',').Select(id => id.Trim()).ToList();

               

                var employeeIdList = salaries.Select(x => x.IdEmployee).ToList();

                // Fetch Employee Details
                var employees = await _dbContext.Employees
       .Where(e => employeeIdList.Contains((int)e.IdEmployee))
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
                        IdEmployeeSalary=salary.IdEmployeeSalary,
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

                await PopulatePayslipPrintDetails(payslips);
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
                salaryEntity.EmailStatus = "SENT";
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

            query.Append(" ORDER BY e.FirstName, e.LastName, es.IdSalaryMonth DESC;");

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


        

        #region EmployeesForSalaryGenerationStatus

        public async Task<IEnumerable<EmployeesForSalaryGenerationStatusDto>> GetEmployeesForSalaryGenerationStatus(int? idSalaryMonth = null)
        {
            try
            {
                var query = from s in _dbContext.EmployeesForSalaryGenerationStatus
                            join e in _dbContext.Employees on s.IdEmployee equals e.IdEmployee into empGroup
                            from e in empGroup.DefaultIfEmpty()
                            join sm in _dbContext.SalaryMonths on s.IdSalaryMonth equals sm.IdSalaryMonth into monthGroup
                            from sm in monthGroup.DefaultIfEmpty()
                            select new { s, e, sm };

                if (idSalaryMonth.HasValue && idSalaryMonth > 0)
                {
                    query = query.Where(x => x.s.IdSalaryMonth == idSalaryMonth.Value);
                }

                return await query
                    .OrderBy(x => x.s.IdSalaryMonth)
                    .ThenBy(x => x.e != null ? x.e.EmployeeCode : null)
                    .Select(x => new EmployeesForSalaryGenerationStatusDto
                    {
                        IdEmployee = x.s.IdEmployee,
                        IdSalaryMonth = x.s.IdSalaryMonth,
                        SalaryGenerationRemarks = x.s.SalaryGenerationRemarks,
                        EmployeeCode = x.e != null ? x.e.EmployeeCode : null,
                        EmployeeName = x.e != null ? (x.e.FirstName + " " + (x.e.MiddleName ?? "") + " " + (x.e.LastName ?? "")).Trim() : null,
                        SalaryMonthText = x.sm != null ? x.sm.SalaryMonthText : null
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching EmployeesForSalaryGenerationStatus.");
                throw;
            }
        }

        public async Task<EmployeesForSalaryGenerationStatusDto?> GetEmployeeForSalaryGenerationStatus(int idEmployee, int idSalaryMonth)
        {
            var records = await GetEmployeesForSalaryGenerationStatus(idSalaryMonth);
            return records.FirstOrDefault(x => x.IdEmployee == idEmployee);
        }

        /// <summary>Inserts the selected employees for a salary month. Throws InvalidOperationException with the user message.</summary>
        public async Task<int> AddEmployeesForSalaryGenerationStatus(List<EmployeesForSalaryGenerationStatusDto> dtos)
        {
            if (dtos.GroupBy(d => new { d.IdEmployee, d.IdSalaryMonth }).Any(g => g.Count() > 1))
                throw new InvalidOperationException("The same employee is listed more than once for the salary month.");

            var employeeIds = dtos.Select(d => d.IdEmployee).Distinct().ToList();
            var monthIds = dtos.Select(d => d.IdSalaryMonth).Distinct().ToList();

            var existingEmployeeIds = await _dbContext.Employees.AsNoTracking()
                .Where(e => e.IdEmployee.HasValue && employeeIds.Contains(e.IdEmployee.Value))
                .Select(e => e.IdEmployee!.Value)
                .ToListAsync();
            var missingEmployees = employeeIds.Except(existingEmployeeIds).ToList();
            if (missingEmployees.Any())
                throw new InvalidOperationException($"Invalid employee(s): {string.Join(", ", missingEmployees)}.");

            var existingMonthIds = await _dbContext.SalaryMonths.AsNoTracking()
                .Where(m => monthIds.Contains(m.IdSalaryMonth))
                .Select(m => m.IdSalaryMonth)
                .ToListAsync();
            var missingMonths = monthIds.Except(existingMonthIds).ToList();
            if (missingMonths.Any())
                throw new InvalidOperationException($"Invalid salary month(s): {string.Join(", ", missingMonths)}.");

            var alreadyAdded = await _dbContext.EmployeesForSalaryGenerationStatus.AsNoTracking()
                .Where(s => employeeIds.Contains(s.IdEmployee) && monthIds.Contains(s.IdSalaryMonth))
                .ToListAsync();
            var duplicates = alreadyAdded
                .Where(a => dtos.Any(d => d.IdEmployee == a.IdEmployee && d.IdSalaryMonth == a.IdSalaryMonth))
                .Select(a => a.IdEmployee)
                .ToList();
            if (duplicates.Any())
                throw new InvalidOperationException($"Employee(s) already added for the salary month: {string.Join(", ", duplicates)}.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var entities = dtos.Select(d => new EmployeesForSalaryGenerationStatus
                {
                    IdEmployee = d.IdEmployee,
                    IdSalaryMonth = d.IdSalaryMonth,
                    SalaryGenerationRemarks = d.SalaryGenerationRemarks
                }).ToList();

                await _dbContext.EmployeesForSalaryGenerationStatus.AddRangeAsync(entities);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                foreach (var idMonth in monthIds)
                {
                    await _auditService.LogAuditAsync("Create", "EmployeesForSalaryGenerationStatus", idMonth,
                        new { after = dtos.Where(d => d.IdSalaryMonth == idMonth) });
                }
                return entities.Count;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding EmployeesForSalaryGenerationStatus.");
                throw new Exception("An error occurred while adding employees for salary generation. Please try again.");
            }
        }

        /// <summary>Updates SalaryGenerationRemarks for existing records. Throws InvalidOperationException if a record does not exist.</summary>
        public async Task<int> UpdateEmployeesForSalaryGenerationStatus(List<EmployeesForSalaryGenerationStatusDto> dtos)
        {
            var employeeIds = dtos.Select(d => d.IdEmployee).Distinct().ToList();
            var monthIds = dtos.Select(d => d.IdSalaryMonth).Distinct().ToList();

            var existing = await _dbContext.EmployeesForSalaryGenerationStatus
                .Where(s => employeeIds.Contains(s.IdEmployee) && monthIds.Contains(s.IdSalaryMonth))
                .ToListAsync();

            var notFound = dtos
                .Where(d => !existing.Any(e => e.IdEmployee == d.IdEmployee && e.IdSalaryMonth == d.IdSalaryMonth))
                .Select(d => $"{d.IdEmployee}/{d.IdSalaryMonth}")
                .ToList();
            if (notFound.Any())
                throw new InvalidOperationException($"Record(s) not found (IdEmployee/IdSalaryMonth): {string.Join(", ", notFound)}.");

            try
            {
                var before = existing.Select(e => new { e.IdEmployee, e.IdSalaryMonth, e.SalaryGenerationRemarks }).ToList();

                foreach (var dto in dtos)
                {
                    var entity = existing.First(e => e.IdEmployee == dto.IdEmployee && e.IdSalaryMonth == dto.IdSalaryMonth);
                    entity.SalaryGenerationRemarks = dto.SalaryGenerationRemarks;
                }
                await _dbContext.SaveChangesAsync();

                foreach (var idMonth in monthIds)
                {
                    await _auditService.LogAuditAsync("Update", "EmployeesForSalaryGenerationStatus", idMonth,
                        new { before = before.Where(b => b.IdSalaryMonth == idMonth), after = dtos.Where(d => d.IdSalaryMonth == idMonth) });
                }
                return dtos.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EmployeesForSalaryGenerationStatus.");
                throw new Exception("An error occurred while updating employees for salary generation. Please try again.");
            }
        }

        public async Task<bool> DeleteEmployeeForSalaryGenerationStatus(int idEmployee, int idSalaryMonth)
        {
            try
            {
                var entity = await _dbContext.EmployeesForSalaryGenerationStatus
                    .FirstOrDefaultAsync(s => s.IdEmployee == idEmployee && s.IdSalaryMonth == idSalaryMonth);
                if (entity == null)
                    return false;

                _dbContext.EmployeesForSalaryGenerationStatus.Remove(entity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync("Delete", "EmployeesForSalaryGenerationStatus", idSalaryMonth,
                    new { before = new { entity.IdEmployee, entity.IdSalaryMonth, entity.SalaryGenerationRemarks } });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmployeesForSalaryGenerationStatus record {IdEmployee}/{IdSalaryMonth}.", idEmployee, idSalaryMonth);
                throw;
            }
        }

        /// <summary>
        /// Deletes all records (or only those of one salary month). Called before inserting a new salary generation selection,
        /// so the table holds only the current session's employees.
        /// </summary>
        public async Task<int> DeleteAllEmployeesForSalaryGenerationStatus(int? idSalaryMonth = null)
        {
            try
            {
                var query = _dbContext.EmployeesForSalaryGenerationStatus.AsQueryable();
                if (idSalaryMonth.HasValue && idSalaryMonth > 0)
                {
                    query = query.Where(s => s.IdSalaryMonth == idSalaryMonth.Value);
                }

                var deletedCount = await query.ExecuteDeleteAsync();

                await _auditService.LogAuditAsync("Delete", "EmployeesForSalaryGenerationStatus", idSalaryMonth ?? 0,
                    new { deletedCount, idSalaryMonth });
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmployeesForSalaryGenerationStatus records.");
                throw;
            }
        }

        #endregion


        #region Salary slip (India layout)

        private static readonly string[] PayslipEarningTypes = { "EARNINGS", "EARNING", "REIMBURSEMENT" };
        private const string PayslipDeductionType = "DEDUCTION";
        private const string PayslipEmployerContributionType = "EMPLOYER_CONTRIBUTION";

        /// <summary>
        /// Fills the fields used by the salary slip PDF (PaySlipGeneratorDto): company details, employee statutory and bank
        /// details, earnings / deductions / employer contributions by head type, and the year-to-date block.
        /// PAN, UAN, PF No. and ESI No. = EmployeeStatutoryDetails. Anything that cannot be mapped stays null (blank).
        /// </summary>
        private async Task PopulatePayslipPrintDetails(List<EmployeePayslipDto> payslips)
        {
            if (payslips == null || !payslips.Any()) return;

            var salaryIds = payslips.Select(p => p.IdEmployeeSalary).Distinct().ToList();
            var salaries = await _dbContext.EmployeeSalaries.AsNoTracking()
                .Where(s => salaryIds.Contains(s.IdEmployeeSalary))
                .ToListAsync();
            var employeeIds = salaries.Select(s => s.IdEmployee).Distinct().ToList();
            var monthIds = salaries.Select(s => s.IdSalaryMonth).Distinct().ToList();

            var employees = await _dbContext.Employees.AsNoTracking()
                .Where(e => e.IdEmployee.HasValue && employeeIds.Contains(e.IdEmployee.Value))
                .ToListAsync();

            var months = await _dbContext.SalaryMonths.AsNoTracking()
                .Where(m => monthIds.Contains(m.IdSalaryMonth))
                .ToListAsync();

            // PAN, UAN, PF No. and ESI No. come from EmployeeStatutoryDetails
            var statutoryDetails = await _dbContext.EmployeeStatutoryDetails.AsNoTracking()
                .Where(s => employeeIds.Contains(s.IdEmployee))
                .ToListAsync();

            var bankAccounts = await (from a in _dbContext.EmployeeBankAccounts
                                      join b in _dbContext.Banks on a.IdBank equals b.IdBank
                                      where employeeIds.Contains(a.IdEmployee)
                                      select new { a.IdEmployee, a.IdEmployeeBankAccount, a.OrderNumber, a.AccountNumber, b.BankName })
                                      .AsNoTracking()
                                      .ToListAsync();

            var locations = await (from p in _dbContext.EmployeeOfficePostings
                                   join o in _dbContext.Offices on p.IdOffice equals o.IdOffice
                                   where p.IsCurrentPosting && employeeIds.Contains(p.IdEmployee)
                                   select new { p.IdEmployee, p.PostingFromDate, o.OfficeName })
                                   .AsNoTracking()
                                   .ToListAsync();

            var details = await (from d in _dbContext.EmployeeSalaryDetails
                                 join h in _dbContext.SalaryHeads on d.IdSalaryHead equals (int?)h.IdSalaryHead into headGroup
                                 from h in headGroup.DefaultIfEmpty()
                                 where d.IdEmployeeSalary.HasValue && salaryIds.Contains(d.IdEmployeeSalary.Value)
                                 select new
                                 {
                                     d.IdEmployeeSalary,
                                     d.SalaryHeadName,
                                     d.SalaryHeadType,
                                     d.Amount,
                                     d.AmountInUSD,
                                     d.YTDAmount,
                                     d.YTDAmountUSD,
                                     d.OrderNumber,
                                     HeadOrderNumber = h != null ? h.OrderNumber : null,
                                     StatutoryType = h != null ? h.StatutoryType : null
                                 })
                                 .AsNoTracking()
                                 .ToListAsync();

            var systemParameters = await _dbContext.SystemParameters.AsNoTracking().ToListAsync();
            string? Parameter(string name) => systemParameters.FirstOrDefault(x => x.ParameterName == name)?.ParameterValue;

            foreach (var payslip in payslips)
            {
                var salary = salaries.FirstOrDefault(s => s.IdEmployeeSalary == payslip.IdEmployeeSalary);
                if (salary == null) continue;

                var employee = employees.FirstOrDefault(e => e.IdEmployee == salary.IdEmployee);
                var month = months.FirstOrDefault(m => m.IdSalaryMonth == salary.IdSalaryMonth);

                // Company
                payslip.CompanyName = Parameter("CompanyName");
                payslip.CompanyAddress = Parameter("CompanyAddress");
                payslip.CompanyRegistrationNumber = Parameter("RegistrationNumber");
                payslip.PayslipColorPattern = Parameter("PAYSLIPCOLORPATTERN");
                payslip.AuthorisedSignatoryName = Parameter("TaxAuthorizedPersonName");
                payslip.AuthorisedSignatureImage = systemParameters
                    .FirstOrDefault(x => x.ParameterName == "TaxAuthorizedSignatureImage")?.ParameterBinaryValue;

                // Pay month and financial year (April to the pay month)
                if (month != null)
                {
                    var monthDate = month.SalaryMonthDate;
                    var financialYearStart = new DateTime(monthDate.Month >= 4 ? monthDate.Year : monthDate.Year - 1, 4, 1);
                    payslip.PayMonthText = monthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                    payslip.YtdPeriodText = $"{financialYearStart.ToString("MMM yyyy", CultureInfo.InvariantCulture)} - {monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture)}".ToUpper();
                }

                // Employee details
                payslip.DateOfJoining = employee?.JoiningDate?.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                payslip.EmploymentType = employee?.EmployeeWorkType;
                var statutory = statutoryDetails.FirstOrDefault(s => s.IdEmployee == salary.IdEmployee);
                payslip.PanNumber = statutory?.PAN;
                payslip.UanNumber = statutory?.UAN;
                payslip.PfNumber = statutory?.PFNumber;
                payslip.EsiNumber = statutory?.ESINumber;
                payslip.Location = locations
                    .Where(l => l.IdEmployee == salary.IdEmployee)
                    .OrderByDescending(l => l.PostingFromDate)
                    .Select(l => l.OfficeName)
                    .FirstOrDefault();

                var bankAccount = bankAccounts
                    .Where(b => b.IdEmployee == salary.IdEmployee)
                    .OrderBy(b => b.OrderNumber ?? int.MaxValue)
                    .ThenBy(b => b.IdEmployeeBankAccount)
                    .FirstOrDefault();
                payslip.BankName = bankAccount?.BankName;
                payslip.BankAccountNumber = MaskAccountNumber(bankAccount?.AccountNumber);

                // Earnings (incl. reimbursements), deductions and employer contributions
                var rows = details
                    .Where(d => d.IdEmployeeSalary == payslip.IdEmployeeSalary)
                    .OrderBy(d => d.OrderNumber ?? d.HeadOrderNumber ?? int.MaxValue)
                    .ToList();

                EmployeeSalaryDetailsDto ToRow(string name, decimal? amount, decimal? amountUsd, decimal? ytd, decimal? ytdUsd) => new EmployeeSalaryDetailsDto
                {
                    Description = name,
                    AmountG = amount ?? 0,
                    AmountUS = amountUsd ?? 0,
                    YTDAmountG = ytd ?? 0,
                    YTDAmountUSD = ytdUsd ?? 0
                };

                var earningRows = rows.Where(r => PayslipEarningTypes.Contains(r.SalaryHeadType)).ToList();
                var deductionRows = rows.Where(r => r.SalaryHeadType == PayslipDeductionType).ToList();
                var employerRows = rows.Where(r => r.SalaryHeadType == PayslipEmployerContributionType).ToList();

                payslip.Earnings = earningRows.Select(r => ToRow(r.SalaryHeadName, r.Amount, r.AmountInUSD, r.YTDAmount, r.YTDAmountUSD)).ToList();
                payslip.Deductions = deductionRows.Select(r => ToRow(r.SalaryHeadName, r.Amount, r.AmountInUSD, r.YTDAmount, r.YTDAmountUSD)).ToList();
                payslip.EmployerContributions = employerRows.Select(r => ToRow(r.SalaryHeadName, r.Amount, r.AmountInUSD, r.YTDAmount, r.YTDAmountUSD)).ToList();

                // Year to date (left blank when the salary run did not store YTD amounts)
                if (rows.Any(r => r.YTDAmount.HasValue))
                {
                    payslip.TotalEarningsYtd = earningRows.Sum(r => r.YTDAmount ?? 0);
                    payslip.TotalDeductionsYtd = deductionRows.Sum(r => r.YTDAmount ?? 0);
                    payslip.NetPayYtd = payslip.TotalEarningsYtd - payslip.TotalDeductionsYtd;
                    payslip.EmployerPfYtd = employerRows.Where(r => r.StatutoryType == "PF_ER_EPF").Sum(r => r.YTDAmount ?? 0);
                    payslip.EmployerEpsYtd = employerRows.Where(r => r.StatutoryType == "PF_ER_EPS").Sum(r => r.YTDAmount ?? 0);
                }
            }
        }

        /// <summary>Shows only the last 4 digits, grouped in fours from the right: "XXXX XXXX XXXX 1234".</summary>
        private static string? MaskAccountNumber(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;

            var value = accountNumber.Replace(" ", string.Empty);
            if (value.Length <= 4) return value;

            var masked = new string('X', value.Length - 4) + value[^4..];
            var groups = new List<string>();
            for (var end = masked.Length; end > 0; end -= 4)
            {
                var start = Math.Max(0, end - 4);
                groups.Insert(0, masked.Substring(start, end - start));
            }
            return string.Join(" ", groups);
        }

        #endregion

    }
}

