using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeServices : IEmployeeServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeServices> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public EmployeeServices(ApplicationDBContext dbContext, IWebHostEnvironment webHostEnvironment, IMapper mapper, ILogger<EmployeeServices> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
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
    e.ChildCountDocumentFilePath,
        bc.BudgetCodeName,
        e.EmployeePhotoFilePath
    FROM 
        Employees e
    INNER JOIN 
        Departments d ON e.IdDepartment = d.IdDepartment
    INNER JOIN 
        Designations des ON e.IdDesignation = des.IdDesignation
 LEFT JOIN 
        BudgetCodes bc ON e.IdBudgetCode = bc.IdBudgetCode
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
                e.ChildCountDocumentFilePath,
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
                if (dto.File != null)
                {
                    string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/Documents");
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }

                    string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                    string fileExtension = Path.GetExtension(dto.File.FileName);
                    string originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(dto.File.FileName);
                    string uniqueFileName = $"CC_{employee.IdEmployee}_{employee.EmployeeCode}_{currentDate}{fileExtension}";
                    string filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                    employee.ChildCountDocumentFilePath = filePath;

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.File.CopyToAsync(stream);
                    }
                }
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

        

       public async Task<bool> DeleteEmployeeAttachment(int EmployeeId)
        {
            try
            {
                // Fetch the employee from the database
                var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.IdEmployee == EmployeeId);
                if (employee == null)
                {
                    _logger.LogWarning($"No details found for Employee ID: {EmployeeId}");
                    return false; // Employee not found
                }
                if (!string.IsNullOrEmpty(employee.ChildCountDocumentFilePath))
                {
                    var filePath = employee.ChildCountDocumentFilePath;

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation($"Deleted file: {filePath}");
                    }
                    else
                    {
                        _logger.LogWarning($"File not found at path: {filePath}");
                    }
                }

                employee.ChildCountDocumentFilePath = null;
             
                // Save the changes
                _dbContext.Employees.Update(employee);
                await _dbContext.SaveChangesAsync();

                return true; // Successfully updated
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Employee ID: {EmployeeId}");
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
             eba.DisbursementType,
             eba.AccountNumber,            
             eba.BranchCode,
             bb.ABARoutingNumber,
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
            e.IdNumber as SSNNumber,
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
                        existingAccount.DisbursementType    = accountDto.DisbursementType;
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
                            DisbursementType= accountDto.DisbursementType,
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
                        existingConfig.DayType = configDto.DayType.ToUpper();
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
                            DayType = configDto.DayType.ToUpper(),
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

        public async Task<IEnumerable<PayslipDetailsDto>> GetPayslipDetails(int idEmployee, int idSalaryMonth)
        {
            var query = new StringBuilder(@"
        SELECT 
            s.IdEmployeeSalary,
            s.IdEmployee,
            s.TotalEarnings,
            s.TotalDeductions,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            des.DesignationName,
            e.IdDepartment,
            d.DepartmentName,
            e.JoiningDate,
            e.Gender,
            e.EmailID,
            e.PhoneNumber1,
            e.PhoneNumber2,
            e.CurrentStatus
        FROM EmployeeSalaries s
        INNER JOIN Employees e ON s.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        WHERE s.IdEmployee = @IdEmployee AND s.IdSalaryMonth = @IdSalaryMonth AND   s.ApprovalStatus = 'APPROVED';
    ");

            var detailQuery = @"
        SELECT 
            d.IdEmployeeSalaryDetail,
            d.SalaryHeadName,
            d.SalaryHeadType AS HeadType,
            d.Amount AS AmountGYD,
            d.AmountInUSD AS AmountUSD,
            d.YTDAmount
        FROM EmployeeSalaryDetails d
        INNER JOIN EmployeeSalaries s ON s.IdEmployeeSalary = d.IdEmployeeSalary
        WHERE s.IdEmployee = @IdEmployee AND s.IdSalaryMonth = @IdSalaryMonth;
    ";

            var parameters = new DynamicParameters();
            parameters.Add("IdEmployee", idEmployee);
            parameters.Add("IdSalaryMonth", idSalaryMonth);

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                var payslip = await connection.QueryAsync<PayslipDetailsDto>(query.ToString(), parameters);
                var details = await connection.QueryAsync<SelfPortalEmployeeSalaryDetailsDto>(detailQuery, parameters);

                var payslipList = payslip.ToList();
                foreach (var p in payslipList)
                {
                    p.EmployeeSalaryDetails = details.ToList();
                }

                return payslipList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Payslip details.");
                throw;
            }
        }

        public async Task<IEnumerable<SalaryDetailsEmployeeDto>> GetSalaryDetailsEmployee(int idEmployee, int idSalaryMonthFrom, int idSalaryMonthTo)
        {
            var query = new StringBuilder(@"
        SELECT 
            e.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDesignation,
            des.DesignationName,
            e.IdDepartment,
            d.DepartmentName,
            e.JoiningDate,
            e.Gender,
            e.EmailID,
            e.PhoneNumber1,
            e.PhoneNumber2,
            e.CurrentStatus,
            s.IdEmployeeSalary,
            s.SalaryMonthText AS SalaryMonthName,
            s.TotalEarnings,
            s.TotalDeductions
        FROM EmployeeSalaries s
        INNER JOIN Employees e ON s.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        WHERE s.IdEmployee = @IdEmployee 
          AND s.IdSalaryMonth BETWEEN @IdSalaryMonthFrom AND @IdSalaryMonthTo
          AND s.ApprovalStatus = 'Approved'
        ORDER BY s.IdSalaryMonth;
    ");

            var parameters = new DynamicParameters();
            parameters.Add("IdEmployee", idEmployee);
            parameters.Add("IdSalaryMonthFrom", idSalaryMonthFrom);
            parameters.Add("IdSalaryMonthTo", idSalaryMonthTo);

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                var data = await connection.QueryAsync<SalaryDetailsEmployeeDto, EmployeeSalariesDto, SalaryDetailsEmployeeDto>(
                    query.ToString(),
                    (employee, salary) =>
                    {
                        if (employee.EmployeeSalaries == null)
                            employee.EmployeeSalaries = new List<EmployeeSalariesDto>();

                        employee.EmployeeSalaries.Add(salary);
                        return employee;
                    },
                    splitOn: "IdEmployeeSalary",
                    param: parameters
                );

                // Group by employee to avoid duplicates
                var grouped = data
                    .GroupBy(e => e.EmployeeCode)
                    .Select(g =>
                    {
                        var emp = g.First();
                        emp.EmployeeSalaries = g.SelectMany(e => e.EmployeeSalaries!).ToList();
                        return emp;
                    });

                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary details for employee.");
                throw;
            }
        }

        public async Task<OTPDto> SetOTP(string emailID)
        {
            try
            {
                var otpDto = new OTPDto();

                var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmailID == emailID);
                if (emp == null)
                {
                    otpDto.IdEmployee = 0;
                    otpDto.OTP = "Not an Authorized Email ID";
                    return otpDto;
                }
                if (emp.CurrentStatus !="Working")
                {
                    otpDto.IdEmployee = 0;
                    otpDto.OTP = "Not an Authorized Email ID";
                    return otpDto;
                }

                var recentOTP = await _dbContext.LoginOTP
                    .Where(o => o.EmailID == emailID && o.OTPSentDate >= DateTime.Now.AddMinutes(-2) && o.OTPLoginStatus == "PENDING")
                    .FirstOrDefaultAsync();

                if (recentOTP != null)
                {
                    otpDto.IdEmployee = 0;
                    otpDto.OTP = "An OTP is already valid and was recently sent. Try again in 1 to 2 minutes.";
                    return otpDto;
                }

                var generatedOtp = new Random().Next(100000, 999999).ToString();

                var otpEntry = new LoginOTP
                {
                    EmailID = emailID,
                    IdEmployee = emp.IdEmployee,
                    OTP = generatedOtp,
                    OTPSentDate = DateTime.Now,
                    OTPLoginStatus = "PENDING"
                };

                await _dbContext.LoginOTP.AddAsync(otpEntry);
                await _dbContext.SaveChangesAsync();

                var otpNotification = await _dbContext.NotificationsConfig
                    .FirstOrDefaultAsync(n => n.NotificationType == "OTP Email");

                if (otpNotification != null)
                {
                    string emailContent = otpNotification.EmailContent
                        .Replace("#EMPLOYEENAME#", $"{emp.FirstName} {emp.LastName}".Trim())
                        .Replace("#OTP#", generatedOtp);

                    await EmailService.SendMail(emailID, otpNotification.EmailSubject, emailContent);
                }

                otpDto.IdEmployee = emp.IdEmployee;
                otpDto.OTP = string.Empty;

                return otpDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating OTP for: {emailID}");
                throw;
            }
        }

        public async Task<OTPStatusDto> ValidateOTP(string emailID, string otp)
        {
            try
            {
                var otpStatusDto = new OTPStatusDto();

                var loginOtp = await _dbContext.LoginOTP
                    .FirstOrDefaultAsync(lo => lo.EmailID == emailID && lo.OTP == otp && lo.OTPLoginStatus == "PENDING");

                if (loginOtp == null)
                {
                    return new OTPStatusDto
                    {
                        IdEmployee = 0,
                        OTPStatus = "Invalid Email ID or OTP"
                    };
                }

                // Mark OTP as used
                loginOtp.OTPLoginStatus = "USED";
                _dbContext.LoginOTP.Update(loginOtp);
                await _dbContext.SaveChangesAsync();

                // Set basic OTP status
                otpStatusDto.IdEmployee = loginOtp.IdEmployee;
                otpStatusDto.OTPStatus = "SUCCESS";

                // Set module access
                otpStatusDto.AuthorizedModules = await _dbContext.EmployeePermissions
                    .AnyAsync(ep => ep.IdEmployee == loginOtp.IdEmployee)
                    ? "PAYROLL,SELFPORTAL"
                    : "SELFPORTAL";

                // Get user details
                var user = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == emailID);
                if (user == null)
                    throw new Exception("User not found.");

                // Get designation
                var designation = await _dbContext.Designations
                    .FirstOrDefaultAsync(x => x.IdDesignation == user.IdDesignation);

                // Generate token
                var token = GenerateToken(user, designation);
                otpStatusDto.Token = new JwtSecurityTokenHandler().WriteToken(token);

                // Load photo blob if exists
                await LoadEmployeePhotoAsync(user);

                // Populate remaining fields
                otpStatusDto.Name = $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim();
                otpStatusDto.Role = designation?.DesignationName ?? string.Empty;
                otpStatusDto.EmployeePhotoFilePath = user.EmployeePhotoFilePath;
                otpStatusDto.AttachmentBlob = user.AttachmentBlob;
                otpStatusDto.Email = user.EmailID;

                return otpStatusDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating OTP for {emailID}");
                throw;
            }
        }
        private async Task LoadEmployeePhotoAsync(Employee user)
        {
            var dbPath = user.EmployeePhotoFilePath?.Trim();
            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
            {
                user.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
            }
        }

        private JwtSecurityToken GenerateToken(Employee user, DesignationEntity designation)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
        new Claim(ClaimTypes.Role, designation?.DesignationName ?? string.Empty),
        new Claim(ClaimTypes.NameIdentifier, user.IdEmployee.ToString()),
        new Claim(ClaimTypes.Email, user.EmailID),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            return GetToken(claims); // Uses your improved token generator
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:ValidIssuer"],
                _configuration["JwtSettings:ValidAudience"],
                expires: DateTime.Now.AddDays(15),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }
}
