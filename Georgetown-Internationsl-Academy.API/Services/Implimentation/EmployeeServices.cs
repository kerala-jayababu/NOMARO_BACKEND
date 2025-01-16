using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

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
            e.ChildrenCount
        FROM 
            Employees e
        INNER JOIN 
            Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN 
            Designations des ON e.IdDesignation = des.IdDesignation
        WHERE 
            e.IdEmployee = @Id;
    ";

            try
            {

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employeeDetails = await connection.QuerySingleOrDefaultAsync<EmployeeDetailsDto>(query, new { Id = id });

                    if (employeeDetails == null)
                    {
                        _logger.LogWarning($"No details found for Employee ID: {id}");
                    }

                    return employeeDetails;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching details for Employee ID: {id} using Dapper.");
                throw;
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
            e.CurrentStatus
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

       

        public async Task<IEnumerable<EmployeeBankAccountDto>> GetEmployeeBankAccountsByID(int Id)
        {
            const string query = @"
        SELECT 
            eba.IdEmployeeBankAccount,
            eba.IdEmployee,
            eba.IdBank,
            eba.AccountNumber,
            eba.BranchName,
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

      
    }
}
