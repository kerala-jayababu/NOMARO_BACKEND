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
           int? idSalaryMonth = null,
           string? dropdownFilter = null,
           int? idDepartment = null,
           int? idDesignation = null)
        {
            var query = new StringBuilder(@"
                WITH LatestSalaryConfig AS (
                    SELECT 
                        IdEmployee,
                        MAX(IdEmployeeSalaryConfig) AS LatestSalaryConfigId
                    FROM vw_LatestEmployeeSalaryConfig
                    GROUP BY IdEmployee
                )
                SELECT 
                    e.IdEmployee,
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
                    e.CurrentStatus,
                    es.IdEmployeeSalary,
                    -- Use EmployeeSalaries data if available, else fallback to vw_LatestEmployeeSalaryConfig
                    COALESCE(es.TotalEarnings, lsc.TotalEarnings, 0) AS TotalEarnings,
                    COALESCE(es.TotalDeductions, lsc.TotalDeductions, 0) AS TotalDeductions,
                      COALESCE(es.TotalEarnings, lsc.TotalEarnings, 0) - COALESCE(es.TotalDeductions, lsc.TotalDeductions, 0) AS NetSalary,
                    COALESCE(es.ApprovalStatus, 'Not Generated') AS ApprovalStatus
                FROM Employees e
                LEFT JOIN Departments dep ON e.IdDepartment = dep.IdDepartment
                LEFT JOIN Designations d ON e.IdDesignation = d.IdDesignation
                LEFT JOIN EmployeeSalaries es ON e.IdEmployee = es.IdEmployee 
                    AND (@IdSalaryMonth IS NULL OR es.IdSalaryMonth = @IdSalaryMonth)
                LEFT JOIN LatestSalaryConfig lsc_max ON e.IdEmployee = lsc_max.IdEmployee
                LEFT JOIN vw_LatestEmployeeSalaryConfig lsc ON lsc.IdEmployeeSalaryConfig = lsc_max.LatestSalaryConfigId
                WHERE e.CurrentStatus = 'Working' ");

            var parameters = new DynamicParameters();

            // Apply Salary Month filter
            if (idSalaryMonth.HasValue)
            {
                parameters.Add("IdSalaryMonth", idSalaryMonth);
            }

            // Apply dropdown filter logic
            if (!string.IsNullOrEmpty(dropdownFilter) && !dropdownFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (dropdownFilter.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(@"
                    AND (
                        es.ApprovalStatus = 'SUBMITTED'
                        OR es.ApprovalStatus = 'INTERIM APPROVED'
                    )");
                }
                else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                {

                    query.Append(" AND es.ApprovalStatus = 'APPROVED' ");
                    //query.Append(@"
                    //AND es.ApprovalStatus = 'APPROVED'
                    //AND es.ValidFrom = (
                    //    SELECT MAX(ValidFrom)
                    //    FROM EmployeeSalaryConfig
                    //    WHERE IdEmployee = es.IdEmployee
                    //    AND ApprovalStatus = 'APPROVED'
                    //)");
                }
                else if (dropdownFilter.Equals("DRAFT GENERATED", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(" AND es.ApprovalStatus = 'Draft' ");
                }
                else if (dropdownFilter.Equals("NOT GENERATED", StringComparison.OrdinalIgnoreCase))
                {
                    query.Append(" AND es.ApprovalStatus IS NULL ");
                }
            }

            // Apply Department filter
            if (idDepartment.HasValue)
            {
                query.Append(" AND e.IdDepartment = @IdDepartment ");
                parameters.Add("IdDepartment", idDepartment);
            }

            // Apply Designation filter
            if (idDesignation.HasValue)
            {
                query.Append(" AND e.IdDesignation = @IdDesignation ");
                parameters.Add("IdDesignation", idDesignation);
            }

            query.Append(" ORDER BY e.FirstName, e.LastName;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var configs = await connection.QueryAsync<SalaryGenerationDto>(query.ToString(), parameters);
                    return configs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary configurations.");
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

                if (!recordsToDelete.Any())
                {
                    return 0; // No records found to delete
                }

                // Remove records
                _dbContext.EmployeeSalaries.RemoveRange(recordsToDelete);
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

        public async Task<List<EmployeePayslipDto>> GeneratePayslipPdf(string employeeIds, int salaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                throw new ArgumentException("Employee ID list cannot be empty.");
            }

            try
            {
                var employeeIdList = employeeIds.Split(',').Select(id => id.Trim()).ToList();

                // Fetch Employee Details
                var employees = await _dbContext.Employees
       .Where(e => employeeIdList.Contains(e.IdEmployee.ToString()))
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

                // Fetch Salary Details for Employees
                var salaries = await _dbContext.EmployeeSalaries
                    .Where(s => employeeIdList.Contains(s.IdEmployee.ToString()) && s.IdSalaryMonth == salaryMonth)
                    .ToListAsync();

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
                            YTDAmountG = 0 // Set to 0 as per requirement
                        })
                        .ToList();

                    var deductions = employeeSalaryDetails
                        .Where(sd => sd.SalaryHeadType == "DEDUCTION")
                        .Select(sd => new EmployeeSalaryDetailsDto
                        {
                            Description = sd.SalaryHeadName,
                            AmountG = sd.Amount ?? 0,
                            AmountUS = sd.AmountInUSD ?? 0,
                            YTDAmountG = 0 // Set to 0 as per requirement
                        })
                        .ToList();

                    // Construct Employee Payslip DTO
                    var payslip = new EmployeePayslipDto
                    {
                        EmployeeCode = employee.EmployeeCode,
                        EmployeeName = employee.FirstName,
                        Position = employee.Position,
                        Department = employee.Department,
                        Period = salary.SalaryMonthText,
                        PayslipGeneratedDate = salary.GeneratedDate.ToString("yyyy-MM-dd"),
                        Earnings = earnings,
                        Deductions = deductions
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

    }
}
