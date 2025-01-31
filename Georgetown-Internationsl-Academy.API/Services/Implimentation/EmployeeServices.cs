using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeServices : IEmployeeServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeServices> _logger;

        public EmployeeServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<EmployeeServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
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
        e.IdDesignation
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


        public async Task<IEnumerable<EmployeeProfileDto>> GetEmployeeList()
        {
            const string query = @"
        SELECT 
            e.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS FullName,
            d.DepartmentName AS Department,
            des.DesignationName AS Designation,
            e.JoiningDate,
            e.CurrentStatus,
            e.IdDepartment,
            e.IdDesignation
        FROM 
            Employees e
        INNER JOIN 
            Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN 
            Designations des ON e.IdDesignation = des.IdDesignation;
    ";

            try
            {
                _logger.LogInformation("Fetching Employee list using Dapper.");

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employees = await connection.QueryAsync<EmployeeProfileDto>(query);
                    return employees;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of employees using Dapper.");
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
            eba.IdBankBranch,
            eba.AccountNumber,            
            eba.BranchCode,
            eba.SalaryPercentageDistributed,
            eba.CurrencyCode
        FROM 
            EmployeeBankAccounts eba
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






    }
}
