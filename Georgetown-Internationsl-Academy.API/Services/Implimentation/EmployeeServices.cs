using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeServices : IEmployeeServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeServices> _logger;
        private readonly IConfiguration _configuration;

        public EmployeeServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<EmployeeServices> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<EmployeeDetailsDto> GetEmployeeDetailsByID(int id)
        {
            const string query = @"
    SELECT 
        e.IdEmployee,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS FullName,
        d.DepartmentName AS Department,
        des.DesignationName AS Designation,
        e.IdBudgetCode,
        e.ChildrenCount,
        e.IdDepartment,
        e.IdDesignation,
        e.EmployeePhotoFilePath
    FROM 
        Employees e
    INNER JOIN 
        Departments d ON e.IdDepartment = d.IdDepartment
    INNER JOIN 
        Designations des ON e.IdDesignation = des.IdDesignation
    WHERE 
        e.IdEmployee = @Id;
    ";

            using (var connection = _dbContext.Database.GetDbConnection())
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employeeDetails = await connection.QuerySingleOrDefaultAsync<EmployeeDetailsDto>(query, new { Id = id });
                    return employeeDetails;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching details for Employee ID: {id} using Dapper.");
                    throw;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        await connection.CloseAsync(); // Close the connection explicitly
                }
            }
        }


        public async Task<IEnumerable<EmployeeProfileDto>> GetEmployeeList(string? searchText = null, DateTime? dateFilter = null) 
        {
                        var query = new StringBuilder(@"
            SELECT 
                e.IdEmployee,
                e.EmployeeCode,
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS FullName,
                d.DepartmentName AS Department,
                des.DesignationName AS Designation,
                e.JoiningDate,
                e.Gender,
                e.EmailID,
                e.PhoneNumber1,
                e.PhoneNumber2,
                e.CurrentStatus,
                e.IdDepartment,
                e.IdDesignation,
                e.EmployeePhotoFilePath   
            FROM Employees e
            INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            WHERE 1=1 "); 

            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(searchText))
            {
                query.Append(@"
    AND (
        e.EmployeeCode LIKE @SearchText
        OR CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText
        OR d.DepartmentName LIKE @SearchText
        OR des.DesignationName LIKE @SearchText
    ) ");
                parameters.Add("SearchText", $"%{searchText}%");
            }

            if (dateFilter.HasValue)
            {
                query.Append(" AND e.JoiningDate >= @DateFilter ");
                parameters.Add("DateFilter", dateFilter.Value.Date); 
            }

            query.Append(" ORDER BY e.FirstName, e.LastName; ");

            try
            {
                _logger.LogInformation("Fetching Employee list with filters using Dapper.");

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employees = await connection.QueryAsync<EmployeeProfileDto>(query.ToString(), parameters);
                    return employees;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the Employee list using Dapper.");
                throw;
            }
        }


        public async Task<bool> UpdateEmployeeDetails(UpdateEmployeeDto dto)
        {
            try
            {
                // Fetch the employee from the database
                var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.IdEmployee == dto.EmployeeId);
                if (employee == null)
                {
                    _logger.LogWarning($"No details found for Employee ID: {dto.EmployeeId}");
                    return false; // Employee not found
                }

                // Update the employee fields
                employee.IdBudgetCode = dto.BudgetCodeId;
                employee.ChildrenCount = dto.ChildCount;

                // Save the changes
                _dbContext.Employees.Update(employee);
                await _dbContext.SaveChangesAsync();

                return true; // Successfully updated
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Employee ID: {dto.EmployeeId}");
                throw;
            }
        }





        public async Task<IEnumerable<EmployeeBankAccountDto>> GetEmployeeBankAccountsByID(int Id)
        {
            const string query = @"
                SELECT 
             eba.IdEmployeeBankAccount,
             eba.IdEmployee,
             eba.IdBank,
	         b.BankName,
             eba.IdBankBranch,
	         bb.BranchName,
             eba.AccountNumber,            
             eba.BranchCode,
             eba.SalaryPercentageDistributed,
             eba.CurrencyCode
         FROM 
             EmployeeBankAccounts eba
	         left join BankBranches bb on eba.IdBankBranch = bb.IdBankBranches
	         left join Banks b on eba.IdBank=b.IdBank
         WHERE 
            eba.IdEmployee = @IdEmployee;
    ";

            try
            {

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var bankAccounts = await connection.QueryAsync<EmployeeBankAccountDto>(query, new { IdEmployee = Id });
                    return bankAccounts;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching bank accounts for Employee ID: {Id} using Dapper.");
                throw;
            }
        }


        public async Task<IEnumerable<EmployeeProfileDetailsDto>> GetEmployeeProfileByID(int Id)
        {
            var query = @"
        SELECT 
            e.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', e.MiddleName, ' ', e.LastName) AS FullName,
            e.EmailId,
            e.PhoneNumber1 AS PhoneNumber1,
            e.PhoneNumber2 AS PhoneNumber2,
            e.WhatsAppNumber AS WhatsAppNumber,
            d.DepartmentName AS Department,
            des.DesignationName AS Designation,
           CONCAT(r.FirstName, ' ', r.MiddleName, ' ', r.LastName) AS ReportingTo,
            b.BudgetCodeName AS BudgetCode,
            e.TaxIdNumber,
            CONCAT(e.Address1, ', ', e.Address2, ',' , e.Address3, ',', e.City, ', ', e.State, ', ', e.ZipCode) AS Address,
            e.CurrentStatus,
            e.JoiningDate,         
            e.EmployeePhotoFilePath,
            e.Gender       -- Include Gender
        FROM dbo.Employees e
        LEFT JOIN dbo.Departments d ON e.IdDepartment = d.IdDepartment
        LEFT JOIN dbo.Designations des ON e.IdDesignation = des.IdDesignation
        LEFT JOIN dbo.Employees r ON e.ReportingTo = r.IdEmployee
        LEFT JOIN dbo.BudgetCodes b ON e.IdBudgetCode = b.IdBudgetCode
        WHERE e.IdEmployee = @Id;
    ";

            try
            {

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employeeProfileDetailsDtos = await connection.QueryAsync<EmployeeProfileDetailsDto>(query, new { Id = Id });
                    return employeeProfileDetailsDtos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching employeeProfileDetails for Employee ID: {Id} using Dapper.");
                throw;
            }
        }


        public async Task<IEnumerable<EmployeeOvertimeConfigDto>> GetEmployeeOvertimeConfigsByID(int employeeId)
        {
            const string query = @"
        SELECT 
            eoc.IdEmployeeOvertimeConfig,
            eoc.IdEmployee,
            eoc.DayType,
            eoc.StandardRate,
            eoc.DayRate
        FROM 
            EmployeeOvertimeConfig eoc
        WHERE 
            eoc.IdEmployee = @IdEmployee;
    ";

            try
            {

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    // Fetch list of overtime configurations
                    var overtimeConfigs = await connection.QueryAsync<EmployeeOvertimeConfigDto>(query, new { IdEmployee = employeeId });
                    return overtimeConfigs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching overtime configurations for Employee ID: {employeeId} using Dapper.");
                throw;
            }
        }


        public async Task<bool> ManageEmployeeBankAccounts(List<EmployeeBankAccountDtoList> bankAccounts)
        {
            if (bankAccounts == null || !bankAccounts.Any())
            {
                throw new ArgumentException("Bank accounts list cannot be null or empty.");
            }

            try
            {
                // Get the Employee ID from the first item in the list
                var employeeId = bankAccounts.First().IdEmployee;

                // Get existing bank accounts for the employee
                var existingAccounts = await _dbContext.EmployeeBankAccounts
                    .Where(x => x.IdEmployee == employeeId)
                    .ToListAsync();

                // Add or Update accounts
                foreach (var accountDto in bankAccounts)
                {
                    var existingAccount = existingAccounts.FirstOrDefault(x => x.IdEmployeeBankAccount == accountDto.IdEmployeeBankAccount);

                    if (existingAccount != null)
                    {
                        // Update existing account
                        existingAccount.IdBank = accountDto.IdBank;
                        existingAccount.IdBankBranch = accountDto.IdBankBranch;
                        existingAccount.AccountNumber = accountDto.AccountNumber;
                        existingAccount.BranchCode = accountDto.BranchCode;
                        existingAccount.SalaryPercentageDistributed = accountDto.SalaryPercentageDistributed;
                        existingAccount.CurrencyCode = accountDto.CurrencyCode.ToUpper().Trim();

                        _dbContext.EmployeeBankAccounts.Update(existingAccount);
                    }
                    else
                    {
                        // Add new account
                        var newAccount = new EmployeeBankAccount
                        {
                            IdEmployee = employeeId,
                            IdBank = accountDto.IdBank,
                            IdBankBranch = accountDto.IdBankBranch,
                            AccountNumber = accountDto.AccountNumber,
                            BranchCode = accountDto.BranchCode,
                            SalaryPercentageDistributed = accountDto.SalaryPercentageDistributed,
                            CurrencyCode = accountDto.CurrencyCode.ToUpper().Trim(),
                        };

                        await _dbContext.EmployeeBankAccounts.AddAsync(newAccount);
                    }
                }

                // Delete accounts that are not in the provided list
                var accountIdsToKeep = bankAccounts
                    .Where(x => x.IdEmployeeBankAccount.HasValue)
                    .Select(x => x.IdEmployeeBankAccount.Value)
                    .ToList();

                var accountsToDelete = existingAccounts
                    .Where(x => !accountIdsToKeep.Contains(x.IdEmployeeBankAccount))
                    .ToList();

                _dbContext.EmployeeBankAccounts.RemoveRange(accountsToDelete);

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing bank accounts for Employee ID: {EmployeeId}", bankAccounts.FirstOrDefault()?.IdEmployee);
                throw;
            }
        }

        public async Task<bool> ManageEmployeeOvertimeConfigs(List<EmployeeOvertimeConfigDtoList> overtimeConfigs)
        {
            try
            {
                if (overtimeConfigs == null || !overtimeConfigs.Any())
                {
                    throw new ArgumentException("Overtime configs list cannot be null or empty.");
                }

                // Extract employeeId from the first DTO in the list
                var employeeId = overtimeConfigs.First().IdEmployee;

                // Get existing overtime configs for the employee
                var existingConfigs = await _dbContext.EmployeeOvertimeConfig
                    .Where(x => x.IdEmployee == employeeId)
                    .ToListAsync();

                // Add or Update configs
                foreach (var configDto in overtimeConfigs)
                {
                    var existingConfig = existingConfigs
                        .FirstOrDefault(x => x.IdEmployeeOvertimeConfig == configDto.IdEmployeeOvertimeConfig);

                    if (existingConfig != null)
                    {
                        // Update existing config
                        existingConfig.DayType = configDto.DayType;
                        existingConfig.StandardRate = configDto.StandardRate;
                        existingConfig.DayRate = configDto.DayRate;
                        _dbContext.EmployeeOvertimeConfig.Update(existingConfig);
                    }
                    else
                    {
                        // Add new config
                        var newConfig = new EmployeeOvertimeConfig
                        {
                            IdEmployee = employeeId,
                            DayType = configDto.DayType,
                            StandardRate = configDto.StandardRate,
                            DayRate = configDto.DayRate
                        };

                        await _dbContext.EmployeeOvertimeConfig.AddAsync(newConfig);
                    }
                }

                // Delete configs that are not in the provided list
                var configIdsToKeep = overtimeConfigs
                    .Where(x => x.IdEmployeeOvertimeConfig.HasValue)
                    .Select(x => x.IdEmployeeOvertimeConfig.Value)
                    .ToList();

                var configsToDelete = existingConfigs
                    .Where(x => !configIdsToKeep.Contains(x.IdEmployeeOvertimeConfig))
                    .ToList();

                _dbContext.EmployeeOvertimeConfig.RemoveRange(configsToDelete);

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing overtime configs for Employee ID: {EmployeeId}", overtimeConfigs.FirstOrDefault()?.IdEmployee);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeHierarchyDto>> GetEmployeesByHierarchy(int employeeId)
        {
            const string designationQuery = @"
    SELECT 
        e.IdEmployee,
        e.IdDesignation,
        des.DesignationCode
    FROM Employees e
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    WHERE e.IdEmployee = @EmployeeId;
";

            const string allEmployeesQuery = @"
    SELECT 
        e.IdEmployee,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        d.DepartmentName,
        e.ReportingTo,
        CONCAT(r.FirstName, ' ', COALESCE(r.MiddleName, ''), ' ', r.LastName) AS IdReportingToName
    FROM Employees e
    INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Employees r ON e.ReportingTo = r.IdEmployee;
";

            const string hierarchyQuery = @"
    SELECT 
        e.IdEmployee,
        e.EmployeeCode,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDesignation,
        des.DesignationName,
        e.IdDepartment,
        d.DepartmentName,
        e.ReportingTo,
        CONCAT(r.FirstName, ' ', COALESCE(r.MiddleName, ''), ' ', r.LastName) AS IdReportingToName
    FROM Employees e
    INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    LEFT JOIN Employees r ON e.ReportingTo = r.IdEmployee
    WHERE e.ReportingTo = @EmployeeId OR e.IdEmployee = @EmployeeId;
";

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    // Step 1: Get the designation of the employee
                    var designation = await connection.QueryFirstOrDefaultAsync(designationQuery, new { EmployeeId = employeeId });
                    if (designation == null)
                        throw new Exception("Employee not found.");

                    // Step 2: Check if the designation code matches
                    var requiredDesignationCode = _configuration["Designations:Code"];
                    if (designation.DesignationCode == requiredDesignationCode)
                    {
                        // Return all employees
                        return await connection.QueryAsync<EmployeeHierarchyDto>(allEmployeesQuery);
                    }

                    // Step 3: If designation does not match, fetch hierarchy under the employee
                    return await connection.QueryAsync<EmployeeHierarchyDto>(hierarchyQuery, new { EmployeeId = employeeId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employees for hierarchy with Employee ID: {employeeId}", employeeId);
                throw;
            }
        }



    }
}
