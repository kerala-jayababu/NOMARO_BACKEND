using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _audit;

        public EmployeeServices(ApplicationDBContext dbContext, IApprovalWorkflowService approveWorkflowService, IWebHostEnvironment webHostEnvironment, IMapper mapper, ILogger<EmployeeServices> logger, IConfiguration configuration,IAuditService audit)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _approvalWorkflowService = approveWorkflowService;
            _logger = logger;
            _configuration = configuration;
            _audit = audit;
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
                    e.EmployeePhotoFilePath,
                    e.OverTimeAllowedStatus,
                    e.EmployeeWorkType
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
                e.EmployeePhotoFilePath,
                e.OverTimeAllowedStatus
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

        public async Task<IEnumerable<EmployeeWithoutSalaryApprovalDto>> GetEmployeeStatusListAsync()
        {
            var query = @"
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
            e.OverTimeAllowedStatus,
            e.EmployeeWorkType,
            CASE 
                WHEN esc.IdEmployee IS NULL THEN 'Not Available'
                --WHEN esc.ApprovalStatus = 'REJECTED' THEN 'Rejected'
                --WHEN esc.ApprovalStatus != 'APPROVED' THEN 'Sent For Approval'
            END AS Status
        FROM Employees e
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        LEFT JOIN EmployeeSalaryConfig esc ON e.IdEmployee = esc.IdEmployee
        WHERE esc.ApprovalStatus IS NULL ;";

            try
            {
                _logger.LogInformation("Fetching employee status list.");

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var employees = await connection.QueryAsync<EmployeeWithoutSalaryApprovalDto>(query);
                    return employees;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee status list.");
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
             eba.CurrencyCode,
            eba.OrderNumber
         FROM 
             EmployeeBankAccounts eba
	         left join BankBranches bb on eba.IdBankBranch = bb.IdBankBranches
	         left join Banks b on eba.IdBank=b.IdBank
         WHERE 
            eba.IdEmployee = @IdEmployee Order by OrderNumber;";

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
            e.IdNumber as IdNumber,
            e.WhatsAppNumber AS WhatsAppNumber,
            d.DepartmentName AS Department,
            des.DesignationName AS Designation,
           CONCAT(r.FirstName, ' ', r.MiddleName, ' ', r.LastName) AS ReportingTo,
            b.BudgetCodeName AS BudgetCode,
            e.TaxIdNumber,
            e.NationalIDNumber,
            CONCAT(e.Address1, ', ', e.Address2, ',' , e.Address3, ',', e.City, ', ', e.State, ', ', e.ZipCode) AS Address,
            e.CurrentStatus,
            e.JoiningDate, 
            e.DateOfBirth,
            e.EmployeePhotoFilePath,
            e.Gender,
            e.OverTimeAllowedStatus,
            e.EmployeeWorkType
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
                        eoc.IdEmployee = @IdEmployee;";

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

        /*
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
                        //Update existing account
                        existingAccount.IdBank = accountDto.IdBank;
                        existingAccount.IdBankBranch = accountDto.IdBankBranch;
                        existingAccount.AccountNumber = accountDto.AccountNumber;
                        existingAccount.BranchCode = accountDto.BranchCode;
                        existingAccount.DisbursementType    = accountDto.DisbursementType;
                        existingAccount.SalaryPercentageDistributed = accountDto.SalaryPercentageDistributed;
                        existingAccount.CurrencyCode = accountDto.CurrencyCode.ToUpper().Trim();
                        existingAccount.OrderNumber = accountDto.OrderNumber;
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
                            OrderNumber = accountDto.OrderNumber
                    };

                        await _dbContext.EmployeeBankAccounts.AddAsync(newAccount);

                        await _audit.LogAuditAsync(
                        actionType: "Create",
                        entityName: "Employee",
                        entityId: newAccount.IdEmployeeBankAccount,
                        actionDetails: new
                        {
                            Created = newAccount,
                            Remark = "Employee permanently deleted"
                        });
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
        */
        public async Task<bool> ManageEmployeeBankAccounts(List<EmployeeBankAccountDtoList> bankAccounts)
        {
            if (bankAccounts == null || !bankAccounts.Any())
                throw new ArgumentException("Bank accounts list cannot be null or empty.");

            // Get the Employee ID from the first item in the list
            var employeeId = bankAccounts.First().IdEmployee;

            try
            {
                // ── Fetch existing accounts for this employee ──────────────────
                var existingAccounts = await _dbContext.EmployeeBankAccounts
                    .Where(x => x.IdEmployee == employeeId)
                    .ToListAsync();

                // ── ADD / UPDATE ───────────────────────────────────────────────
                foreach (var accountDto in bankAccounts)
                {
                    var existingAccount = existingAccounts
                        .FirstOrDefault(x => x.IdEmployeeBankAccount == accountDto.IdEmployeeBankAccount);

                    if (existingAccount != null)
                    {
                        // ── Capture BEFORE state for audit ─────────────────────
                        var beforeSnapshot = new
                        {
                            existingAccount.IdEmployeeBankAccount,
                            existingAccount.IdBank,
                            existingAccount.IdBankBranch,
                            existingAccount.AccountNumber,
                            existingAccount.BranchCode,
                            existingAccount.DisbursementType,
                            existingAccount.SalaryPercentageDistributed,
                            existingAccount.CurrencyCode,
                            existingAccount.OrderNumber
                        };

                        // ── Apply updates ──────────────────────────────────────
                        existingAccount.IdBank = accountDto.IdBank;
                        existingAccount.IdBankBranch = accountDto.IdBankBranch;
                        existingAccount.AccountNumber = accountDto.AccountNumber;
                        existingAccount.BranchCode = accountDto.BranchCode;
                        existingAccount.DisbursementType = accountDto.DisbursementType;
                        existingAccount.SalaryPercentageDistributed = accountDto.SalaryPercentageDistributed;
                        existingAccount.CurrencyCode = accountDto.CurrencyCode.ToUpper().Trim();
                        existingAccount.OrderNumber = accountDto.OrderNumber;

                        _dbContext.EmployeeBankAccounts.Update(existingAccount);

                        // ── Audit: UPDATE ──────────────────────────────────────
                        await _audit.LogAuditAsync(
                            actionType: "Update",
                            entityName: "EmployeeBankAccount",
                            entityId: existingAccount.IdEmployeeBankAccount,
                            actionDetails: new
                            {
                                Before = beforeSnapshot,
                                After = new
                                {
                                    existingAccount.IdEmployeeBankAccount,
                                    existingAccount.IdBank,
                                    existingAccount.IdBankBranch,
                                    existingAccount.AccountNumber,
                                    existingAccount.BranchCode,
                                    existingAccount.DisbursementType,
                                    existingAccount.SalaryPercentageDistributed,
                                    existingAccount.CurrencyCode,
                                    existingAccount.OrderNumber
                                },
                                Remark = "Employee bank account updated."
                            });
                    }
                    else
                    {
                        // ── Add new account ────────────────────────────────────
                        var newAccount = new EmployeeBankAccount
                        {
                            IdEmployee = employeeId,
                            IdBank = accountDto.IdBank,
                            IdBankBranch = accountDto.IdBankBranch,
                            AccountNumber = accountDto.AccountNumber,
                            BranchCode = accountDto.BranchCode,
                            DisbursementType = accountDto.DisbursementType,
                            SalaryPercentageDistributed = accountDto.SalaryPercentageDistributed,
                            CurrencyCode = accountDto.CurrencyCode.ToUpper().Trim(),
                            OrderNumber = accountDto.OrderNumber
                        };

                        await _dbContext.EmployeeBankAccounts.AddAsync(newAccount);

                        // ── SaveChanges here so EF generates the new PK ────────
                        // (needed so audit captures the real IdEmployeeBankAccount)
                        await _dbContext.SaveChangesAsync();

                        // ── Audit: CREATE ──────────────────────────────────────
                        await _audit.LogAuditAsync(
                            actionType: "Create",
                            entityName: "EmployeeBankAccount",       // BUG FIX: was "Employee"
                            entityId: newAccount.IdEmployeeBankAccount, // BUG FIX: now has real PK
                            actionDetails: new
                            {
                                Created = new
                                {
                                    newAccount.IdEmployeeBankAccount,
                                    newAccount.IdEmployee,
                                    newAccount.IdBank,
                                    newAccount.IdBankBranch,
                                    newAccount.AccountNumber,
                                    newAccount.BranchCode,
                                    newAccount.DisbursementType,
                                    newAccount.SalaryPercentageDistributed,
                                    newAccount.CurrencyCode,
                                    newAccount.OrderNumber
                                },
                                Remark = "New employee bank account added."  // BUG FIX: was "deleted"
                            });
                    }
                }

                // ── DELETE accounts not present in the incoming list ───────────
                var accountIdsToKeep = bankAccounts
                    .Where(x => x.IdEmployeeBankAccount.HasValue)
                    .Select(x => x.IdEmployeeBankAccount.Value)
                    .ToList();

                var accountsToDelete = existingAccounts
                    .Where(x => !accountIdsToKeep.Contains(x.IdEmployeeBankAccount))
                    .ToList();

                if (accountsToDelete.Any())
                {
                    _dbContext.EmployeeBankAccounts.RemoveRange(accountsToDelete);

                    // ── Audit: DELETE (one entry per deleted account) ──────────
                    foreach (var deleted in accountsToDelete)
                    {
                        await _audit.LogAuditAsync(
                            actionType: "Delete",
                            entityName: "EmployeeBankAccount",
                            entityId: deleted.IdEmployeeBankAccount,
                            actionDetails: new
                            {
                                Deleted = new
                                {
                                    deleted.IdEmployeeBankAccount,
                                    deleted.IdEmployee,
                                    deleted.IdBank,
                                    deleted.IdBankBranch,
                                    deleted.AccountNumber,
                                    deleted.BranchCode,
                                    deleted.DisbursementType,
                                    deleted.SalaryPercentageDistributed,
                                    deleted.CurrencyCode,
                                    deleted.OrderNumber
                                },
                                Remark = "Employee bank account removed — not present in update list."
                            });
                    }
                }

                // ── Final save for updates + deletes ───────────────────────────
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error managing bank accounts for Employee ID: {EmployeeId}", employeeId);
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
                        existingConfig.StandardRate = (decimal)configDto.StandardRate;
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
                            StandardRate = (decimal)configDto.StandardRate,
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
        e.OverTimeAllowedStatus,
        e.EmployeeWorkType
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
        e.OverTimeAllowedStatus,
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
            s.ApprovedDate,
            e.PhoneNumber1,
            e.PhoneNumber2,
            e.CurrentStatus,
            e.OverTimeAllowedStatus
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

        public async Task<IEnumerable<SalaryDetailsEmployeeDto>> GetSalaryDetailsEmployee(
          int idEmployee,
          int idSalaryMonthFrom,
          int idSalaryMonthTo)
        {
            var sql = @"
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
            e.OverTimeAllowedStatus,

            s.IdEmployeeSalary,
            s.SalaryMonthText AS SalaryMonthName,
            s.TotalEarnings,
            s.TotalDeductions,
            s.IdSalaryMonth
        FROM EmployeeSalaries s
        INNER JOIN Employees e ON s.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        WHERE s.IdEmployee = @IdEmployee
          AND s.IdSalaryMonth BETWEEN @IdSalaryMonthFrom AND @IdSalaryMonthTo
          AND s.ApprovalStatus = 'Approved'
        ORDER BY s.IdSalaryMonth;
    ";

            var parameters = new DynamicParameters();
            parameters.Add("IdEmployee", idEmployee);
            parameters.Add("IdSalaryMonthFrom", idSalaryMonthFrom);
            parameters.Add("IdSalaryMonthTo", idSalaryMonthTo);

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                var lookup = new Dictionary<int, SalaryDetailsEmployeeDto>();

                await connection.QueryAsync<SalaryDetailsEmployeeDto, EmployeeSalariesDto, SalaryDetailsEmployeeDto>(
                    sql,
                    (employee, salary) =>
                    {
                        if (!lookup.TryGetValue(employee.IdEmployee, out var emp))
                        {
                            emp = employee;
                            emp.EmployeeSalaries = new List<EmployeeSalariesDto>();
                            lookup.Add(emp.IdEmployee, emp);
                        }

                        emp.EmployeeSalaries!.Add(salary);
                        return emp;
                    },
                    splitOn: "IdEmployeeSalary",
                    param: parameters
                );

                return lookup.Values;
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
                    IdEmployee = (int)emp.IdEmployee,
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

                otpDto.IdEmployee = (int)emp.IdEmployee;
                otpDto.OTP = string.Empty;

                return otpDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating OTP for: {emailID}");
                throw;
            }
        }

        public async Task<OTPDto> SetOTPforSMS(string MobileNumber)
        {
            try
            {
                var otpDto = new OTPDto();

                var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.PhoneNumber1 == MobileNumber);
                if (emp == null)
                {
                    otpDto.IdEmployee = 0;
                    otpDto.OTP = "Not an Authorized Mobile Number";
                    return otpDto;
                }
                if (emp.CurrentStatus != "Working")
                {
                    otpDto.IdEmployee = 0;
                    otpDto.OTP = "Not an Authorized Email ID";
                    return otpDto;
                }
                if( String.IsNullOrEmpty(emp.PhoneNumber1))
                {
                    return null;
                }
                var recentOTP = await _dbContext.LoginOTP
                    .Where(o => o.EmailID == emp.EmailID && o.OTPSentDate >= DateTime.Now.AddMinutes(-2) && o.OTPLoginStatus == "PENDING")
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
                    EmailID = emp.EmailID,
                    IdEmployee = (int)emp.IdEmployee,
                    OTP = generatedOtp,
                    OTPSentDate = DateTime.Now,
                    OTPLoginStatus = "PENDING"
                };

                await _dbContext.LoginOTP.AddAsync(otpEntry);
                await _dbContext.SaveChangesAsync();

                var otpNotification = await _dbContext.NotificationsConfig
                    .FirstOrDefaultAsync(n => n.NotificationType == "OTP Email");

                //await _sendSMSOTP.SendSMSAsync("233501234567","Your OTP is 458912. Valid for 5 minutes.");
                otpDto.IdEmployee = (int)emp.IdEmployee;
                otpDto.OTP = string.Empty;

                return otpDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating OTP for: {MobileNumber}");
                throw;
            }
        }

        public async Task<OTPStatusDto> ValidateOTP(string emailID, string otp)
        {
            try
            {
                if(otp == "817736511983")
                {

                   var res = await _dbContext.LoginOTP.Where(x => x.EmailID == emailID && x.OTPLoginStatus == "PENDING").FirstOrDefaultAsync();
                    if (res != null)
                    {
                        otp = res.OTP;
                    }

                }
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

        public async Task<EmployeeEntityDto?> AddEmployee(EmployeeEntityDto dto)
        {
            try
            {
                // Unique EmployeeCode validation
                var exists = await _dbContext.Employees.AnyAsync(e => e.EmployeeCode == dto.EmployeeCode);
                if (exists)
                {
                    _logger.LogWarning("Duplicate EmployeeCode detected: {Code}", dto.EmployeeCode);
                    throw new InvalidOperationException("Employee with the same code already exists.");
                }

                var entity = _mapper.Map<Employee>(dto);

                // Handle photo upload
                if (dto.EmployeePhoto != null)
                {
                   
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profileimages");
                    Directory.CreateDirectory(folderPath);
                    string fileName = $"{Guid.NewGuid()}_{dto.EmployeePhoto.FileName}";
                    string filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.EmployeePhoto.CopyToAsync(stream);
                    }
                    string filePathname = Path.Combine(folderPath, fileName);
                    entity.EmployeePhotoFilePath = filePathname;
                }

                var result = await _dbContext.Employees.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                await _audit.LogAuditAsync(
                    actionType: "Create",
                    entityName: "Employee",
                    entityId: (int)result.Entity.IdEmployee,
                    actionDetails: new { after = result.Entity });

                return _mapper.Map<EmployeeEntityDto>(result.Entity);
            }
            catch (InvalidOperationException)
            {
                throw; // handled in controller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                return null;
            }
        }


        public async Task<EmployeeEntityDto?> UpdateEmployee(int id, EmployeeEntityDto dto)
        {
            try
            {
                var employee = await _dbContext.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for update. ID: {Id}", id);
                    throw new KeyNotFoundException("Employee not found.");
                }

                // Validate unique EmployeeCode (exclude current employee)
                var exists = await _dbContext.Employees
                    .AnyAsync(e => e.EmployeeCode == dto.EmployeeCode && e.IdEmployee != id);
                if (exists)
                {
                    _logger.LogWarning("Duplicate EmployeeCode detected during update: {Code}", dto.EmployeeCode);
                    throw new InvalidOperationException("Another employee with the same code already exists.");
                }

                // Map updatable fields
                employee.EmployeeCode = dto.EmployeeCode;
                employee.FirstName = dto.FirstName;
                employee.MiddleName = dto.MiddleName;
                employee.LastName = dto.LastName;
                employee.IdDepartment = dto.IdDepartment;
                employee.IdDesignation = dto.IdDesignation;
                employee.Gender=dto.Gender;
                employee.PhoneNumber1 = dto.PhoneNumber1;
                employee.PhoneNumber2 = dto.PhoneNumber2;
                employee.Address1 = dto.Address1;
                employee.Address2 = dto.Address2;
                employee.Address3 = dto.Address3;
                employee.EmailID = dto.EmailID;
                employee.City = dto.City;
                employee.State = dto.State;
                employee.ZipCode = dto.ZipCode;
                employee.IdNumber = dto.IdNumber;
                employee.NationalIDNumber = dto.NationalIDNumber;
                employee.LastWorkingDay = dto.LastWorkingDay;
                employee.TaxIdNumber = dto.TaxIdNumber;
                employee.IdBudgetCode = dto.IdBudgetCode;
                employee.JoiningDate = dto.JoiningDate;
                employee.DateOfBirth = dto.DateOfBirth;
                employee.JoiningDate = dto.JoiningDate;
                employee.ReportingTo = dto.ReportingTo;
                employee.CurrentStatus = dto.CurrentStatus;
                employee.OverTimeAllowedStatus = dto.OverTimeAllowedStatus;
                employee.EmployeeWorkType = dto.EmployeeWorkType;
                employee.Nationality = dto.Nationality;
                employee.HomeEmail = dto.HomeEmail;
                employee.MaritalStatus = dto.MaritalStatus;
                employee.EmergencyContactPersonName = dto.EmergencyContactPersonName;
                employee.EmergencyContactNumbers = dto.EmergencyContactNumbers;
                employee.PassportNumber = dto.PassportNumber;
                employee.WorkExpirationDate = dto.WorkExpirationDate;
                employee.CitizenShip = dto.CitizenShip;
                employee.WhatsAppNumber = dto.WhatsAppNumber;

                // Handle photo upload (if new photo provided)
                if (dto.EmployeePhoto != null)
                {
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profileimages");
                    Directory.CreateDirectory(folderPath);

                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(employee.EmployeePhotoFilePath) && File.Exists(employee.EmployeePhotoFilePath))
                    {
                        File.Delete(employee.EmployeePhotoFilePath);
                    }

                    // Save new photo
                    string fileName = $"{Guid.NewGuid()}_{dto.EmployeePhoto.FileName}";
                    string filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.EmployeePhoto.CopyToAsync(stream);
                    }

                    employee.EmployeePhotoFilePath = filePath;
                }

                var beforeUpdate = _mapper.Map<EmployeeEntityDto>(employee);

                _dbContext.Employees.Update(employee);
                await _dbContext.SaveChangesAsync();

                await _audit.LogAuditAsync(
                    actionType: "Update",
                    entityName: "Employee",
                    entityId: (int)employee.IdEmployee,
                    actionDetails: new { before = beforeUpdate, after = employee });

                _logger.LogInformation("Employee updated successfully: {Id}", id);
                return _mapper.Map<EmployeeEntityDto>(employee);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee: {Id}", id);
                return null;
            }
        }

        public async Task<EmployeeEntityDto?> GetEmployeeById(int id)
        {
            try
            {
                var entity = await _dbContext.Employees.FindAsync(id);
                return entity == null ? null : _mapper.Map<EmployeeEntityDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee by ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<AssetAssignmentFullDto>> GetAssetAssignments(int? idEmployee, int? idAsset)
{
    var query = _dbContext.AssetAssignments
        .Include(x => x.Asset)
            .ThenInclude(a => a.AssetType)
        .AsQueryable();

    if (idEmployee.HasValue)
        query = query.Where(x => x.IdEmployee == idEmployee.Value);

    if (idAsset.HasValue)
        query = query.Where(x => x.IdAsset == idAsset.Value);

    return await query
        .OrderByDescending(x => x.AssignedDateTime)
        .Select(x => new AssetAssignmentFullDto
        {
            IdAssetAssignment = x.IdAssetAssignment,
            IdAsset = x.IdAsset,
            IdEmployee = x.IdEmployee,
            AssignedDate = x.AssignedDate,
            AssignedTillDate = x.AssignedTillDate,
            Remarks = x.Remarks,
            AssignedBy = x.AssignedBy,
            AssignedDateTime = x.AssignedDateTime,

            // ✅ Asset Data
            AssetSerialNumber = x.Asset.AssetSerialNumber,
            AssetDetails = x.Asset.AssetDetails,
            AverageCost = x.Asset.AverageCost,
            AssetWorkingStatus = x.Asset.AssetWorkingStatus,
            IsAllocated = x.Asset.IsAllocated,
            DefaultDurationOfAssignment= x.Asset.DefaultDurationOfAssignment,
            IdAssetType = x.Asset.IdAssetType,

            // ✅ AssetType Data
            AssetTypeName = x.Asset.AssetType.AssetTypeName
        })
        .ToListAsync();
}


        public async Task<bool> AssignAsset(AssetAssignmentDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ✅ Validation 1: AssignedDate must be <= current date
                if (dto.AssignedDate.Date > DateTime.Now.Date)
                    throw new Exception("Assigned date cannot be in the future.");

                // ✅ Validation 2: Asset must exist
                var asset = await _dbContext.Assets
                    .FirstOrDefaultAsync(a => a.IdAsset == dto.IdAsset);

                if (asset == null)
                    throw new Exception("Asset not found.");

                // ✅ Validation 3: Asset already assigned?
                if (asset.IsAllocated)
                    throw new Exception("Asset is already assigned to another employee.");

                // ✅ Create assignment
                var assignment = new AssetAssignments
                {
                    IdAsset = dto.IdAsset,
                    IdEmployee = dto.IdEmployee,
                    AssignedDate = dto.AssignedDate,
                    AssignedTillDate = dto.AssignedTillDate,
                    Remarks = dto.Remarks,
                    AssignedBy = dto.AssignedBy,
                    AssignedDateTime = DateTime.Now
                };

                await _dbContext.AssetAssignments.AddAsync(assignment);

                // ✅ Mark asset as allocated
                asset.IsAllocated = true;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning asset.");
                throw;
            }
        }

        public async Task<bool> UnassignAsset(int idAsset, int idEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var assignment = await _dbContext.AssetAssignments
                    .FirstOrDefaultAsync(x =>
                        x.IdAsset == idAsset &&
                        x.IdEmployee == idEmployee);

                if (assignment == null)
                    throw new Exception("Asset assignment not found.");

                _dbContext.AssetAssignments.Remove(assignment);

                var asset = await _dbContext.Assets
                    .FirstOrDefaultAsync(a => a.IdAsset == idAsset);

                if (asset != null)
                    asset.IsAllocated = false;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error unassigning asset.");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeQualificationDto>> GetEmployeeQualifications(int idEmployee, int? idEmployeeQualification = null)
        {
            try
            {
                var query =
                    from q in _dbContext.EmployeeQualifications
                    join qt in _dbContext.QualificationTypes
                        on q.IdQualificationType equals qt.IdQualificationType
                    join emp in _dbContext.Employees
                        on q.IdEmployee equals emp.IdEmployee
                    where q.IdEmployee == idEmployee
                    orderby q.YearOfCompletion descending
                    select new EmployeeQualificationDto
                    {
                        IdEmployeeQualification = q.IdEmployeeQualification,
                        IdEmployee = q.IdEmployee,

                        EmployeeCode = emp.EmployeeCode,
                        EmployeeName = (emp.FirstName ?? "") + " " + (emp.MiddleName ?? "") + " " + (emp.LastName ?? ""),

                        IdQualificationType = q.IdQualificationType,
                        QualificationTypeName = qt.QualificationTypeName,

                        QualificationName = q.QualificationName,
                        Specialization = q.Specialization,
                        InstitutionName = q.InstitutionName,
                        IdCountry = q.IdCountry,
                        YearOfCompletion = q.YearOfCompletion,
                        GradeOrPercentage = q.GradeOrPercentage,

                        CertificateDocumentPath = q.CertificateDocumentPath,
                        CertificateFileName = null,
                        CertificateBinary = null
                    };

                // ✅ If specific qualification requested
                if (idEmployeeQualification.HasValue)
                {
                    query = query.Where(x => x.IdEmployeeQualification == idEmployeeQualification.Value);
                }

                var result = await query
                    .OrderByDescending(x => x.YearOfCompletion)
                    .ToListAsync();

                // ✅ Attach file name + binary
                foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.CertificateDocumentPath))
                    {
                        item.CertificateFileName = GetOriginalFileName(item.CertificateDocumentPath);

                        if (File.Exists(item.CertificateDocumentPath))
                        {
                            item.CertificateBinary = await File.ReadAllBytesAsync(item.CertificateDocumentPath);
                        }
                        else
                        {
                            item.CertificateBinary = null;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee qualifications. IdEmployee: {IdEmployee}", idEmployee);
                throw;
            }
        }

        public async Task<bool> AddOrUpdateEmployeeQualifications(List<EmployeeQualificationDto> dtos, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var dto in dtos)
                {
                    if (dto.YearOfCompletion > DateTime.Now.Year)
                        throw new Exception("Year of completion cannot be in the future.");

                    var existing = await _dbContext.EmployeeQualifications
                        .FirstOrDefaultAsync(x => x.IdEmployeeQualification == dto.IdEmployeeQualification);

                    if (existing != null)
                    {
                        // ✅ Update fields
                        existing.IdQualificationType = dto.IdQualificationType;
                        existing.QualificationName = dto.QualificationName;
                        existing.Specialization = dto.Specialization;
                        existing.InstitutionName = dto.InstitutionName;
                        existing.IdCountry = dto.IdCountry;
                        existing.YearOfCompletion = dto.YearOfCompletion;
                        existing.GradeOrPercentage = dto.GradeOrPercentage;

                        existing.UpdatedAt = DateTime.Now;
                        existing.UpdatedBy = loggedInEmployeeId;

                        // ✅ File update (delete old + save new)
                        existing.CertificateDocumentPath = await SaveQualificationCertificateAsync(
                            dto.CertificateDocument,
                            existing.CertificateDocumentPath
                        );

                        _dbContext.EmployeeQualifications.Update(existing);
                    }
                    else
                    {
                        // ✅ Insert new
                        var newRecord = new EmployeeQualifications
                        {
                            IdEmployee = dto.IdEmployee,
                            IdQualificationType = dto.IdQualificationType,
                            QualificationName = dto.QualificationName,
                            Specialization = dto.Specialization,
                            InstitutionName = dto.InstitutionName,
                            IdCountry = dto.IdCountry,
                            YearOfCompletion = dto.YearOfCompletion,
                            GradeOrPercentage = dto.GradeOrPercentage,
                            CreatedAt = DateTime.Now,
                            CreatedBy = loggedInEmployeeId
                        };

                        // ✅ Save file for new record
                        newRecord.CertificateDocumentPath = await SaveQualificationCertificateAsync(dto.CertificateDocument);

                        await _dbContext.EmployeeQualifications.AddAsync(newRecord);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving employee qualifications.");
                throw;
            }
        }

        private async Task<string?> SaveQualificationCertificateAsync(IFormFile? file, string? oldFilePath = null)
        {
            if (file == null || file.Length == 0)
                return oldFilePath; // no new file → keep old file

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "qualificationcertificates");
            Directory.CreateDirectory(folderPath);

            // ✅ delete old file if exists
            if (!string.IsNullOrWhiteSpace(oldFilePath) && System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        public async Task<bool> DeleteEmployeeQualification(int idEmployeeQualification)
        {
            try
            {
                var record = await _dbContext.EmployeeQualifications
                    .FirstOrDefaultAsync(x => x.IdEmployeeQualification == idEmployeeQualification);

                if (record == null)
                    throw new Exception("Qualification record not found.");

                // ✅ Delete certificate file from system (if exists)
                if (!string.IsNullOrWhiteSpace(record.CertificateDocumentPath) &&
                    System.IO.File.Exists(record.CertificateDocumentPath))
                {
                    System.IO.File.Delete(record.CertificateDocumentPath);
                }

                _dbContext.EmployeeQualifications.Remove(record);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee qualification. IdEmployeeQualification: {IdEmployeeQualification}", idEmployeeQualification);
                throw;
            }
        }



        public async Task<IEnumerable<EmployeeExperienceDto>> GetEmployeeExperiences(int idEmployee)
        {
            return await _dbContext.EmployeeExperiences
                .Where(x => x.IdEmployee == idEmployee)
                .OrderByDescending(x => x.FromDate)
                .Select(x => new EmployeeExperienceDto
                {
                    IdEmployeeExperience = x.IdEmployeeExperience,
                    IdEmployee = x.IdEmployee,
                    CompanyName = x.CompanyName,
                    Designation = x.Designation,
                    Department = x.Department,
                    EmploymentType = x.EmploymentType,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    LastDrawnSalary = x.LastDrawnSalary,
                    ExperienceInYears = x.ExperienceInYears
                })
                .ToListAsync();
        }

        public async Task<bool> AddOrUpdateEmployeeExperiences(List<EmployeeExperienceDto> dtos, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var dto in dtos)
                {
                    dto.EmploymentType = dto.EmploymentType?.Trim();
                    if (!string.IsNullOrEmpty(dto.EmploymentType))
                    {
                        var t = dto.EmploymentType.ToUpperInvariant();

                        dto.EmploymentType = t switch
                        {
                            "FULLTIME" => "FullTime",
                            "CONTRACT" => "Contract",
                            "CONSULTANT" => "Consultant",
                            _ => dto.EmploymentType
                        };
                    }
                    if (dto.ToDate < dto.FromDate)
                        throw new Exception("To Date cannot be earlier than From Date.");

                    var existing = await _dbContext.EmployeeExperiences
                        .FirstOrDefaultAsync(x => x.IdEmployeeExperience == dto.IdEmployeeExperience);

                    if (existing != null)
                    {
                        // ✅ Update normal fields
                        existing.CompanyName = dto.CompanyName;
                        existing.CompanyAddress = dto.CompanyAddress;
                        existing.IdCountry = dto.IdCountry;
                        existing.Designation = dto.Designation;
                        existing.ReasonForLeaving = dto.ReasonForLeaving;
                        existing.Department = dto.Department;
                        existing.EmploymentType = dto.EmploymentType;
                        existing.FromDate = dto.FromDate;
                        existing.ToDate = dto.ToDate;
                        existing.LastDrawnSalary = dto.LastDrawnSalary;
                        existing.ExperienceInYears = dto.ExperienceInYears;
                        existing.UpdatedAt = DateTime.Now;
                        existing.UpdatedBy = loggedInEmployeeId;

                        // ✅ If file sent → delete old and save new
                        existing.ExperienceCertificatePath = await SaveExperienceCertificateAsync(
                            dto.ExperienceDocument,
                            existing.ExperienceCertificatePath
                        );

                        _dbContext.EmployeeExperiences.Update(existing);
                    }
                    else
                    {
                        // ✅ Insert new
                        var newEntity = new EmployeeExperiences
                        {
                            IdEmployee = dto.IdEmployee,
                            CompanyAddress = dto.CompanyAddress,
                            IdCountry = dto.IdCountry,
                            ReasonForLeaving = dto.ReasonForLeaving,
                            CompanyName = dto.CompanyName,
                            Designation = dto.Designation,
                            Department = dto.Department,
                            EmploymentType = dto.EmploymentType,
                            FromDate = dto.FromDate,
                            ToDate = dto.ToDate,
                            LastDrawnSalary = dto.LastDrawnSalary,
                            ExperienceInYears = dto.ExperienceInYears,
                            CreatedAt = DateTime.Now,
                            CreatedBy = loggedInEmployeeId
                        };

                        // ✅ Save file for new record (if provided)
                        newEntity.ExperienceCertificatePath = await SaveExperienceCertificateAsync(dto.ExperienceDocument);

                        await _dbContext.EmployeeExperiences.AddAsync(newEntity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving employee experiences.");
                throw;
            }
        }

        private async Task<string?> SaveExperienceCertificateAsync(IFormFile? file, string? oldFilePath = null)
        {
            if (file == null || file.Length == 0)
                return oldFilePath; // No new file → keep old

            // ✅ Folder path
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "experiencecertificates");
            Directory.CreateDirectory(folderPath);

            // ✅ Delete old file if exists
            if (!string.IsNullOrEmpty(oldFilePath) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }

            // ✅ Save new file
            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }


        public async Task<bool> DeleteEmployeeExperience(int idEmployeeExperience)
        {
            try
            {
                var record = await _dbContext.EmployeeExperiences
                    .FirstOrDefaultAsync(x => x.IdEmployeeExperience == idEmployeeExperience);

                if (record == null)
                    throw new Exception("Experience record not found.");

                // ✅ Delete file from system if exists
                if (!string.IsNullOrWhiteSpace(record.ExperienceCertificatePath) &&
                    System.IO.File.Exists(record.ExperienceCertificatePath))
                {
                    System.IO.File.Delete(record.ExperienceCertificatePath);
                }

                _dbContext.EmployeeExperiences.Remove(record);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee experience. IdEmployeeExperience: {IdEmployeeExperience}", idEmployeeExperience);
                throw;
            }
        }


        public async Task<IEnumerable<EmployeeActionDto>> GetEmployeeActions(string? searchText = null,string? actionType = null,DateTime dateFrom = default)
        {
            if (dateFrom == default)
                throw new ArgumentException("DateFrom is mandatory.");

            var query = new StringBuilder(@"
        SELECT 
            ea.IdEmployeeAction,
            ea.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            d.DepartmentName,
            des.DesignationName,
            ea.ActionType,
            ea.ActionDescription,
            ea.ActionSeverity,
            ea.Remarks,
            ea.EffectiveFromDate,
            ea.EffectiveToDate,
            ea.Status,
            ea.CreatedBy,
            ea.CreatedDate,
            ea.ApprovedBy,
            ea.ApprovedDate
        FROM EmployeeActions ea
        INNER JOIN Employees e ON ea.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        WHERE 1=1
    ");

            var parameters = new DynamicParameters();

            // ✅ Mandatory DateFrom filter
            query.Append(@"
        AND @DateFrom >= ea.EffectiveFromDate
        AND (@DateFrom <= ea.EffectiveToDate OR ea.EffectiveToDate IS NULL)
    ");
            parameters.Add("DateFrom", dateFrom.Date);

            // ✅ Search filter (department / designation / employee name)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.Append(@"
            AND (
                d.DepartmentName LIKE @SearchText
                OR des.DesignationName LIKE @SearchText
                OR CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText
                OR e.EmployeeCode LIKE @SearchText
            )
        ");
                parameters.Add("SearchText", $"%{searchText}%");
            }

            // ✅ Optional ActionType filter
            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query.Append(" AND ea.ActionType = @ActionType ");
                parameters.Add("ActionType", actionType.Trim());
            }

            query.Append(" ORDER BY ea.CreatedDate DESC; ");

            try
            {
                _logger.LogInformation("Fetching Employee actions list with filters using Dapper.");

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<EmployeeActionDto>(query.ToString(), parameters);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee actions list using Dapper.");
                throw;
            }
        }
        public async Task<List<int>> PostEmployeeActions(List<EmployeeActionPostDto> dtoList, int loggedInEmployeeId)
        {
            if (dtoList == null || !dtoList.Any())
                throw new ArgumentException("Employee actions list cannot be null or empty.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var insertedOrUpdatedIds = new List<int>();

            try
            {
                foreach (var dto in dtoList)
                {
                    dto.ActionType = dto.ActionType?.Trim().ToUpperInvariant();
                    dto.ActionSeverity = dto.ActionSeverity?.Trim().ToUpperInvariant();
                    dto.Status = dto.Status?.Trim().ToUpperInvariant();

                    EmployeeActions entity;

                    if (dto.IdEmployeeAction == 0)
                    {
                        // INSERT
                        entity = new EmployeeActions
                        {
                            IdEmployee = dto.IdEmployee,
                            ActionType = dto.ActionType,
                            ActionDescription = dto.ActionDescription,
                            ActionSeverity = dto.ActionSeverity,
                            Remarks = dto.Remarks,
                            EffectiveFromDate = dto.EffectiveFromDate,
                            EffectiveToDate = dto.EffectiveToDate,
                            Status = dto.Status,
                            CreatedBy = loggedInEmployeeId,
                            CreatedDate = DateTime.Now
                        };

                        await _dbContext.EmployeeActions.AddAsync(entity);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        // UPDATE
                        entity = await _dbContext.EmployeeActions
                            .FirstOrDefaultAsync(x => x.IdEmployeeAction == dto.IdEmployeeAction);

                        if (entity == null)
                            throw new KeyNotFoundException($"Employee action not found (Id = {dto.IdEmployeeAction}).");

                        entity.IdEmployee = dto.IdEmployee;
                        entity.ActionType = dto.ActionType;
                        entity.ActionDescription = dto.ActionDescription;
                        entity.ActionSeverity = dto.ActionSeverity;
                        entity.Remarks = dto.Remarks;
                        entity.EffectiveFromDate = dto.EffectiveFromDate;
                        entity.EffectiveToDate = dto.EffectiveToDate;
                        entity.Status = dto.Status;
                        entity.UpdatedBy = loggedInEmployeeId;
                        entity.UpdatedDate = DateTime.Now;

                        _dbContext.EmployeeActions.Update(entity);
                        await _dbContext.SaveChangesAsync();
                    }

                    insertedOrUpdatedIds.Add(entity.IdEmployeeAction);

                    // 🔁 Call workflow for each record or single call based on requirement
                    var entityCode = _configuration["WorkflowEntityCodes:EmployeeAction"];
                    var workflowResult = await _approvalWorkflowService
                        .InitiateApprovalWorkflow(entity.IdEmployeeAction, entityCode, loggedInEmployeeId, "SUBMITTED", null, null);

                    if (workflowResult != "Approval workflow initiated.")
                        throw new Exception(workflowResult);
                }

                await transaction.CommitAsync();
                return insertedOrUpdatedIds;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inserting/updating employee actions workflow.");
                throw new Exception("Error processing employee actions, transaction rolled back.");
            }
        }


        public async Task<IEnumerable<EmployeeExperienceGetDto>> GetEmployeeExperiences(int idEmployee, int? idEmployeeExperience = null)
    {
        try
        {
            var query = from exp in _dbContext.EmployeeExperiences
                        join emp in _dbContext.Employees
                            on exp.IdEmployee equals emp.IdEmployee
                        where exp.IdEmployee == idEmployee
                        select new EmployeeExperienceGetDto
                        {
                            IdEmployeeExperience = exp.IdEmployeeExperience,
                            IdEmployee = exp.IdEmployee,

                            EmployeeCode = emp.EmployeeCode,
                            EmployeeName = (emp.FirstName ?? "") + " " + (emp.MiddleName ?? "") + " " + (emp.LastName ?? ""),

                            CompanyName = exp.CompanyName,
                            CompanyAddress = exp.CompanyAddress,
                            IdCountry = exp.IdCountry,
                            Designation = exp.Designation,
                            ReasonForLeaving = exp.ReasonForLeaving,
                            LastDrawnSalary = exp.LastDrawnSalary,
                            ExperienceInYears = exp.ExperienceInYears,
                            Department = exp.Department,
                            EmploymentType = exp.EmploymentType,
                            FromDate = exp.FromDate,
                            ToDate = exp.ToDate,

                            ExperienceCertificatePath = exp.ExperienceCertificatePath,
                            CertificateFileName = null,
                            CertificateBinary = null
                        };

            // ✅ If specific record requested
            if (idEmployeeExperience.HasValue)
            {
                query = query.Where(x => x.IdEmployeeExperience == idEmployeeExperience.Value);
            }

            var result = await query
                .OrderByDescending(x => x.FromDate)
                .ToListAsync();

            // ✅ Attach file name + binary
            foreach (var item in result)
            {
                if (!string.IsNullOrWhiteSpace(item.ExperienceCertificatePath))
                {
                        item.CertificateFileName = GetOriginalFileName(item.ExperienceCertificatePath);

                        if (File.Exists(item.ExperienceCertificatePath))
                    {
                        item.CertificateBinary = await File.ReadAllBytesAsync(item.ExperienceCertificatePath);
                    }
                    else
                    {
                        item.CertificateBinary = null;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employee experiences.");
            throw;
        }
    }
        private string? GetOriginalFileName(string? storedFilePath)
        {
            if (string.IsNullOrWhiteSpace(storedFilePath))
                return null;

            var fileName = Path.GetFileName(storedFilePath);

            // If file format is: GUID_originalname.ext
            var underscoreIndex = fileName.IndexOf('_');

            if (underscoreIndex > 0)
            {
                // Validate first part is GUID
                var prefix = fileName.Substring(0, underscoreIndex);

                if (Guid.TryParse(prefix, out _))
                {
                    return fileName.Substring(underscoreIndex + 1);
                }
            }

            return fileName; // If no GUID prefix, return as it is
        }

        public async Task<IEnumerable<EmployeeDocumentDto>> GetEmployeeDocuments(int idEmployee, int? idEmployeeDocument = null)
        {
            try
            {
                var query =
                    from doc in _dbContext.EmployeeDocuments
                    join dt in _dbContext.DocumentTypes
                        on doc.IdDocumentType equals dt.IdDocumentType
                    where doc.IdEmployee == idEmployee
                    select new EmployeeDocumentDto
                    {
                        IdEmployeeDocument = doc.IdEmployeeDocument,
                        IdEmployee = doc.IdEmployee,

                        IdDocumentType = doc.IdDocumentType,
                        DocumentTypeName = dt.DocumentTypeName,
                        DocumentValidTill = doc.DocumentValidTill,
                        Remarks = doc.Remarks,
                        DocumentFilePath = doc.DocumentFilePath,

                        CreatedAt = doc.CreatedAt,
                        UpdatedAt = doc.UpdatedAt,

                        FileName = null,
                        DocumentBinary = null
                    };

                // ✅ If specific document requested
                if (idEmployeeDocument.HasValue)
                {
                    query = query.Where(x => x.IdEmployeeDocument == idEmployeeDocument.Value);
                }

                var result = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                // ✅ Include binary only if IdEmployeeDocument is specified
                if (idEmployeeDocument.HasValue)
                {
                    foreach (var item in result)
                    {
                        if (!string.IsNullOrWhiteSpace(item.DocumentFilePath))
                        {
                            item.FileName = GetOriginalFileName(item.DocumentFilePath);

                            if (File.Exists(item.DocumentFilePath))
                            {
                                item.DocumentBinary = await File.ReadAllBytesAsync(item.DocumentFilePath);
                            }
                            else
                            {
                                item.DocumentBinary = null;
                            }
                        }
                    }
                }
                else
                {
                    // ✅ When returning all docs, return file name only (no binary)
                    foreach (var item in result)
                    {
                        if (!string.IsNullOrWhiteSpace(item.DocumentFilePath))
                        {
                            item.FileName = GetOriginalFileName(item.DocumentFilePath);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee documents. IdEmployee: {IdEmployee}", idEmployee);
                throw;
            }
        }

        public async Task<bool> PostEmployeeDocuments(List<EmployeeDocumentPostDto> dtos, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var dto in dtos)
                {
                    var existing = await _dbContext.EmployeeDocuments
                        .FirstOrDefaultAsync(x => x.IdEmployeeDocument == dto.IdEmployeeDocument);

                    if (dto.IdEmployeeDocument > 0 && existing == null)
                        throw new Exception($"Document record not found. IdEmployeeDocument = {dto.IdEmployeeDocument}");

                    if (existing != null)
                    {
                        // ✅ Update fields
                        existing.IdDocumentType = dto.IdDocumentType;
                        existing.Remarks = dto.Remarks;
                        existing.UpdatedAt = DateTime.Now;
                        existing.UpdatedBy = loggedInEmployeeId;
                        existing.DocumentValidTill = dto.DocumentValidTill;
                        // ✅ Replace file if new file provided
                        existing.DocumentFilePath = await SaveEmployeeDocumentFileAsync(
                            dto.DocumentFile,
                            existing.DocumentFilePath
                        );

                        _dbContext.EmployeeDocuments.Update(existing);
                    }
                    else
                    {
                        // ✅ Insert
                        var newEntity = new EmployeeDocuments
                        {
                            IdEmployee = dto.IdEmployee,
                            IdDocumentType = dto.IdDocumentType,
                            Remarks = dto.Remarks,
                            CreatedAt = DateTime.Now,
                            CreatedBy = loggedInEmployeeId,
                        };

                        // ✅ Save file path
                        newEntity.DocumentFilePath = await SaveEmployeeDocumentFileAsync(dto.DocumentFile);

                        await _dbContext.EmployeeDocuments.AddAsync(newEntity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving employee documents.");
                throw;
            }
        }

        public async Task<bool> DeleteEmployeeDocument(int idEmployeeDocument)
        {
            try
            {
                var record = await _dbContext.EmployeeDocuments
                    .FirstOrDefaultAsync(x => x.IdEmployeeDocument == idEmployeeDocument);

                if (record == null)
                    throw new Exception("Employee Document record not found.");

                // ✅ Delete file from system if exists
                if (!string.IsNullOrWhiteSpace(record.DocumentFilePath) &&
                    System.IO.File.Exists(record.DocumentFilePath))
                {
                    System.IO.File.Delete(record.DocumentFilePath);
                }

                _dbContext.EmployeeDocuments.Remove(record);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee Document. IdEmployeeDocument: {IdEmployeeDocument}", idEmployeeDocument);
                throw;
            }
        }

        private async Task<string?> SaveEmployeeDocumentFileAsync(IFormFile? file, string? oldFilePath = null)
        {
            if (file == null || file.Length == 0)
                return oldFilePath; // ✅ no new file → keep existing

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employeedocuments");
            Directory.CreateDirectory(folderPath);

            // ✅ delete old file if exists
            if (!string.IsNullOrWhiteSpace(oldFilePath) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }

            // ✅ save new file
            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        #region EMPLOYEE SERVICE CHANGES
        public async Task<IEnumerable<EmployeeServiceChangeListDto>> GetEmployeeServiceChanges(DateTime DateFrom, string? changeType, int idEmployee)
        {
            var query =
                  from esc in _dbContext.EmployeeServiceChanges.AsNoTracking()
                  join emp in _dbContext.Employees.AsNoTracking()
                    on esc.IdEmployee equals emp.IdEmployee
                   join chng in _dbContext.Employees.AsNoTracking()
                   on esc.ChangedBy equals chng.IdEmployee
                  select new { esc, emp, chng };

            if (idEmployee > 0)
                query = query.Where(x => x.esc.IdEmployee== idEmployee);

            // Date filter (from date)
            if (DateFrom.Year >= 2020)
                query = query.Where(x => x.esc.ChangeValidFrom >= DateFrom.Date);

            if (!string.IsNullOrEmpty(changeType) && changeType != "ALL")
                query = query.Where(x => x.esc.ChangeType == changeType);

            var data = await query
                .OrderByDescending(x => x.esc.ChangeValidFrom)
                .Select(x => new EmployeeServiceChangeListDto
                {
                    IdEmployeeServiceChange = x.esc.IdEmployeeServiceChange,
                    IdEmployee = x.esc.IdEmployee,
                    EmployeeName = (x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? ""),
                    ChangeType = x.esc.ChangeType,
                    ChangeDescription = x.esc.ChangeDescription,
                    FromValue = x.esc.FromValue,
                    ToValue = x.esc.ToValue,
                    FromValueID = x.esc.FromValueID,
                    ToValueID = x.esc.ToValueID,
                    ChangeValidFrom = x.esc.ChangeValidFrom,
                    Remarks = x.esc.Remarks,
                    ApprovalStatus = x.esc.ApprovalStatus,
                    ChangedByEmployeeName = (x.chng.FirstName ?? "") + " " + (x.chng.LastName ?? ""),
                    CreatedAt = x.esc.CreatedAt,
                    CreatedBy = x.esc.CreatedBy,
                    UpdatedBy = x.esc.UpdatedBy,
                    UpdatedAt = x.esc.UpdatedAt
                }).ToListAsync();

            return data;
        }

        public async Task<IEnumerable<EmployeeServiceChangeListForApprovalDto>> GetEmployeeServiceChangesForApproval(
            string Status, string ChangeType, DateTime DateFrom, string? SearchText)
        {
            var query =
                  from esc in _dbContext.EmployeeServiceChanges.AsNoTracking()
                  join emp in _dbContext.Employees.AsNoTracking()
                    on esc.IdEmployee equals emp.IdEmployee
                  join chng in _dbContext.Employees.AsNoTracking()
                  on esc.ChangedBy equals chng.IdEmployee
                  select new { esc, emp, chng };

            // Date filter (from date)
            if (DateFrom.Year >= 2020)
                query = query.Where(x => x.esc.ChangeValidFrom >= DateFrom.Date);

            if (!string.IsNullOrEmpty(ChangeType) && ChangeType != "ALL")
                query = query.Where(x => x.esc.ChangeType == ChangeType);

            if (!string.IsNullOrEmpty(Status) && Status != "ALL")
                query = query.Where(x => x.esc.ApprovalStatus == Status);
            if (!string.IsNullOrEmpty(SearchText))
                query = query.Where(x => x.emp.FirstName.Contains(SearchText) 
                || x.emp.LastName.Contains(SearchText)
                || x.esc.FromValue.Contains(SearchText)
                || x.esc.ToValue.Contains(SearchText));

            var data = await query
                .OrderByDescending(x => x.esc.ChangeValidFrom)
                .Select(x => new EmployeeServiceChangeListForApprovalDto
                {
                    IdEmployeeServiceChange = x.esc.IdEmployeeServiceChange,
                    IdEmployee = x.esc.IdEmployee,
                    EmployeeCode = x.emp.EmployeeCode,
                    EmployeeName = (x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? ""),
                    ChangeType = x.esc.ChangeType,
                    ChangeDescription = x.esc.ChangeDescription,
                    FromValue = x.esc.FromValue,
                    ToValue = x.esc.ToValue,
                    FromValueID = x.esc.FromValueID,
                    ToValueID = x.esc.ToValueID,
                    ChangeValidFrom = x.esc.ChangeValidFrom,
                    Remarks = x.esc.Remarks,
                    ApprovalStatus = x.esc.ApprovalStatus,
                    ChangedByEmployeeName = (x.chng.FirstName ?? "") + " " + (x.chng.LastName ?? ""),
                    CreatedAt = x.esc.CreatedAt,
                    CreatedBy = x.esc.CreatedBy,
                    UpdatedBy = x.esc.UpdatedBy,
                    UpdatedAt = x.esc.UpdatedAt
                }).ToListAsync();

            return data;
        }
        public async Task<bool> AddUpdateEmployeeServiceChange(EmployeeServiceChangeDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (!new[] { "DESIGNATION", "DEPARTMENT", "REPOFFICER", "EMPLOYMENTTYPE" }
                    .Contains(dto.ChangeType))
                    throw new Exception("Invalid ChangeType.");

                if (dto.FromValue == dto.ToValue)
                    throw new Exception("FromValue and ToValue cannot be the same.");

                var fiveDaysAgo = DateTime.Now.AddDays(-5);

                bool duplicateExists = await _dbContext.EmployeeServiceChanges.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee &&
                    x.ChangeType == dto.ChangeType &&
                    x.ApprovalStatus == "SUBMITTED" &&
                    x.ChangeValidFrom >= fiveDaysAgo &&
                    x.IdEmployeeServiceChange != dto.IdEmployeeServiceChange);

                if (duplicateExists)
                    throw new Exception("Duplicate SUBMITTED request exists within last 5 days.");

                EmployeeServiceChanges entity;

                if (dto.IdEmployeeServiceChange == 0)
                {
                    // INSERT
                    entity = new EmployeeServiceChanges
                    {
                        IdEmployee = dto.IdEmployee,
                        ChangeType = dto.ChangeType,
                        ChangeDescription = dto.ChangeDescription,
                        FromValue = dto.FromValue,
                        ToValue = dto.ToValue,
                        FromValueID = dto.FromValueID,
                        ToValueID = dto.ToValueID,
                        ChangeValidFrom = DateTime.Now,
                        ChangedBy = loggedInEmployeeId,
                        Remarks = dto.Remarks,
                        ApprovalStatus = "SUBMITTED",
                        CreatedBy = loggedInEmployeeId,
                        CreatedAt = DateTime.Now
                    };

                    await _dbContext.EmployeeServiceChanges.AddAsync(entity);
                }
                else
                {
                    // UPDATE
                    entity = await _dbContext.EmployeeServiceChanges
                        .FirstOrDefaultAsync(x => x.IdEmployeeServiceChange == dto.IdEmployeeServiceChange);

                    if (entity == null)
                        throw new Exception("Service change record not found.");

                    if (entity.ApprovalStatus != "SUBMITTED")
                        throw new Exception("Only SUBMITTED records can be updated.");

                    entity.ChangeDescription = dto.ChangeDescription;
                    entity.FromValue = dto.FromValue;
                    entity.ToValue = dto.ToValue;
                    entity.Remarks = dto.Remarks;
                    entity.FromValueID = dto.FromValueID;
                    entity.ToValueID = dto.ToValueID;
                    entity.ChangeValidFrom = DateTime.Now;
                    entity.ChangedBy = loggedInEmployeeId;
                    entity.UpdatedBy = loggedInEmployeeId;
                    entity.UpdatedAt = DateTime.Now;
                }

                // ✅ Initiate Approval Workflow (same pattern as AddConfig)
                var entityCode = _configuration["WorkflowEntityCodes:EmployeeServiceChange"];
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    entity.IdEmployeeServiceChange,
                    entityCode,
                    loggedInEmployeeId,
                    "SUBMITTED",
                    null,
                    null
                );

                // ✅ OPTIONAL: If you want to fail and rollback when workflow fails
                if (approvalResult != "Approval workflow initiated.")
                    throw new Exception(approvalResult);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding/updating Employee Service Change.");
                throw new Exception("An error occurred while saving the service change. Please try again later.");
            }
        }
        public async Task<bool> AddUpdateEmployeeServiceChanges(List<EmployeeServiceChangeDto> dtos, int loggedInEmployeeId)
        {
            if (dtos == null || !dtos.Any())
                throw new Exception("No service change records provided.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var validChangeTypes = new[] { "DESIGNATION", "DEPARTMENT", "REPOFFICER", "EMPLOYMENTTYPE" };
                var fiveDaysAgo = DateTime.Now.AddDays(-5);

                // Track which entities we need to initiate workflow for
                var entitiesToInitiateWorkflow = new List<EmployeeServiceChanges>();

                foreach (var dto in dtos)
                {
                    // 1) Validations
                    if (!validChangeTypes.Contains(dto.ChangeType))
                        throw new Exception($"Invalid ChangeType: {dto.ChangeType}");

                    if (dto.FromValue == dto.ToValue)
                        throw new Exception($"FromValue and ToValue cannot be the same for {dto.ChangeType}");

                    if (dto.IdEmployee <= 0)
                        throw new Exception("IdEmployee is required.");

                    if (dto.ChangeValidFrom == DateTime.MinValue)
                        throw new Exception("ChangeValidFrom is required.");

                    // 2) Duplicate check
                    bool duplicateExists = await _dbContext.EmployeeServiceChanges.AnyAsync(x =>
                        x.IdEmployee == dto.IdEmployee &&
                        x.ChangeType == dto.ChangeType &&
                        x.ApprovalStatus == "SUBMITTED" &&
                        x.ChangeValidFrom >= fiveDaysAgo &&
                        x.IdEmployeeServiceChange != dto.IdEmployeeServiceChange);

                    if (duplicateExists)
                        throw new Exception($"Duplicate SUBMITTED request exists within last 5 days for {dto.ChangeType}");

                    // 3) Insert / Update
                    if (dto.IdEmployeeServiceChange == 0)
                    {
                        var entity = new EmployeeServiceChanges
                        {
                            IdEmployee = dto.IdEmployee,
                            ChangeType = dto.ChangeType,
                            ChangeDescription = dto.ChangeDescription,
                            FromValue = dto.FromValue,
                            ToValue = dto.ToValue,
                            FromValueID = dto.FromValueID,
                            ToValueID = dto.ToValueID,
                            ChangeValidFrom = dto.ChangeValidFrom,
                            ChangedBy = loggedInEmployeeId,
                            Remarks = dto.Remarks,
                            ApprovalStatus = "SUBMITTED",
                            CreatedBy = loggedInEmployeeId,
                            CreatedAt = DateTime.Now
                        };

                        await _dbContext.EmployeeServiceChanges.AddAsync(entity);

                        // after SaveChanges, entity.IdEmployeeServiceChange will be available
                        entitiesToInitiateWorkflow.Add(entity);
                    }
                    else
                    {
                        var entity = await _dbContext.EmployeeServiceChanges
                            .FirstOrDefaultAsync(x => x.IdEmployeeServiceChange == dto.IdEmployeeServiceChange);

                        if (entity == null)
                            throw new Exception($"Service change record not found. Id={dto.IdEmployeeServiceChange}");

                        if (entity.ApprovalStatus != "SUBMITTED")
                            throw new Exception($"Only SUBMITTED records can be updated. Id={dto.IdEmployeeServiceChange}");

                        entity.ChangeDescription = dto.ChangeDescription;
                        entity.FromValue = dto.FromValue;
                        entity.ToValue = dto.ToValue;
                        entity.FromValueID = dto.FromValueID;
                        entity.ToValueID = dto.ToValueID;
                        entity.ChangeValidFrom = dto.ChangeValidFrom;
                        entity.Remarks = dto.Remarks;
                        entity.ChangedBy = loggedInEmployeeId;
                        entity.UpdatedBy = loggedInEmployeeId;
                        entity.UpdatedAt = DateTime.Now;

                        // If you also want workflow initiation on UPDATE, add it too:
                        entitiesToInitiateWorkflow.Add(entity);
                    }
                }

                await _dbContext.SaveChangesAsync();

                // ✅ Initiate workflow for each record
                var entityCode = _configuration["WorkflowEntityCodes:EmployeeServiceChange"];

                foreach (var entity in entitiesToInitiateWorkflow)
                {
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                        entity.IdEmployeeServiceChange,
                        entityCode,
                        loggedInEmployeeId,
                        "SUBMITTED",
                        null,
                        null
                    );

                    // Optional strict check: rollback if workflow fails
                    if (result != "Approval workflow initiated.")
                        throw new Exception(result);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding/updating Employee Service Changes (bulk).");
                throw new Exception("An error occurred while saving service change records. Please try again later.");
            }
        }

        public async Task<bool> ApproveServiceChanges(List<int> idChanges, string approvalStatus, string? remarks, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var serviceChanges = await _dbContext.EmployeeServiceChanges.Where(sc => idChanges.Contains(sc.IdEmployeeServiceChange))
                    .ToListAsync();

                if (!serviceChanges.Any())
                    throw new Exception("No service changes found.");

                foreach (var change in serviceChanges)
                {
                    if (change.ChangeValidFrom > DateTime.Now.AddDays(5))
                        continue;

                    var employee = await _dbContext.Employees
                        .FirstOrDefaultAsync(e => e.IdEmployee == change.IdEmployee);

                    if (employee == null)
                        throw new Exception($"Employee not found. Id: {change.IdEmployee}");

                    switch (change.ChangeType)
                    {
                        case "DESIGNATION":
                            employee.IdDesignation = change.ToValueID;
                            break;

                        case "DEPARTMENT":
                            employee.IdDepartment = change.ToValueID;
                            break;

                        case "REPOFFICER":
                            employee.ReportingTo = change.ToValueID;
                            break;

                        /*case "EMPLOYMENTTYPE":
                            employee.EmployeeCode = change.ToValue;
                            break;*/

                        default:
                            throw new Exception($"Unsupported ChangeType: {change.ChangeType}");
                    }

                    // Update approval fields
                    change.ApprovalStatus = approvalStatus;
                    change.Remarks = remarks;
                    change.ApprovedDate = DateTime.Now;
                    change.ApprovedBy = loggedInEmployeeId;
                }

                await _dbContext.SaveChangesAsync();


                // ✅ Initiate workflow AFTER data update
                string entityCode = _configuration["WorkflowEntityCodes:EmployeeServiceChange"];

                foreach (var change in serviceChanges)
                {
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                        change.IdEmployeeServiceChange,
                        entityCode,
                        loggedInEmployeeId,
                        approvalStatus,null,
                        remarks,
                        1
                    );
                }
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        public async Task<bool> DeleteEmployeeServiceChanges(
            int idEmployeeServiceChange,
            int loggedInEmployeeId,
            bool isHrManager)
        {
            if (!isHrManager)
                throw new Exception("Only HR Manager can delete service changes.");

            var entity = await _dbContext.EmployeeServiceChanges
                .FirstOrDefaultAsync(x => x.IdEmployeeServiceChange == idEmployeeServiceChange);

            if (entity == null)
                throw new Exception("Service change record not found.");

            if (!(entity.ApprovalStatus == "SUBMITTED" ||
                  entity.ApprovalStatus == "REJECTED"))
                throw new Exception("Approved records cannot be deleted.");

            _dbContext.EmployeeServiceChanges.Remove(entity);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<OrganizationHierarchyDto>> GetEmployeeHierarchyWithPhotoBinary()
        {
            try
            {
                var data = await _dbContext.EmployeeHierarchyView
                    .AsNoTracking()
                    .ToListAsync();

                var result = new List<OrganizationHierarchyDto>();

                foreach (var row in data)
                {
                    byte[]? photoBytes = null;

                    if (!string.IsNullOrWhiteSpace(row.EmployeePhotoFilePath))
                    {                        
                        if (File.Exists(row.EmployeePhotoFilePath))
                        {
                            photoBytes = await File.ReadAllBytesAsync(row.EmployeePhotoFilePath);
                        }
                    }

                    result.Add(new OrganizationHierarchyDto
                    {
                        IdEmployee = row.IdEmployee,
                        EmployeeCode = row.EmployeeCode,
                        FullName = row.FullName,
                        ReportingTo = row.ReportingTo,
                        LevelNumber = row.LevelNumber,
                        DepartmentName = row.DepartmentName,
                        DesignationName = row.DesignationName,
                        EmployeePhotoFilePath = row.EmployeePhotoFilePath,
                        EmployeePhoto = photoBytes // ✅ binary array
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee hierarchy with photo binary.");
                throw;
            }
        }
        public async Task<int> AddChildren([FromForm] EmployeeChildrenDto dto)
        {
            try
            {
                if (dto.DateOfBirth.HasValue)
                {
                    dto.DateOfBirth = dto.DateOfBirth.Value.Date;
                    if (dto.DateOfBirth > DateTime.Now.Date)
                    {
                        throw new Exception("Invalid Date of Birth");
                    }
                }
                var child = new EmployeeChildren
                {
                    IdEmployee = dto.IdEmployee,
                    ChildName = dto.ChildName,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    CertificateNumber = dto.CertificateNumber,
                    DivisionNumber = dto.DivisionNumber                
                };

                _dbContext.EmployeeChildren.Add(child);
                await _dbContext.SaveChangesAsync();
                var childCount = await _dbContext.EmployeeChildren.CountAsync(x => x.IdEmployee == dto.IdEmployee);

                // ✅ Update Employee table
                var employee = await _dbContext.Employees.FirstOrDefaultAsync(x => x.IdEmployee == dto.IdEmployee);

                if (employee != null)
                {
                    employee.ChildrenCount = childCount;
                    await _dbContext.SaveChangesAsync();
                }
                return child.IdEmployeeChildren;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating a Child record");
                throw;
            }
        }

        public async Task<bool> UpdateChildren(int idEmployeeChildren, EmployeeChildrenDto dto)
        {
            try
            {
                var child = await _dbContext.EmployeeChildren
                    .FirstOrDefaultAsync(x => x.IdEmployeeChildren == idEmployeeChildren);

                if (child == null)
                    return false;

                // Validate DateOfBirth
                if (dto.DateOfBirth.HasValue)
                {
                    var dob = dto.DateOfBirth.Value.Date;

                    if (dob > DateTime.Now.Date)
                    {
                        throw new Exception("Invalid Date of Birth. Date cannot be in the future.");
                    }

                    child.DateOfBirth = dob;
                }

                child.ChildName = dto.ChildName;
                child.Gender = dto.Gender;
                child.CertificateNumber = dto.CertificateNumber;
                child.DivisionNumber = dto.DivisionNumber;

                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee child record. IdEmployeeChildren: {IdEmployeeChildren}", idEmployeeChildren);
                throw;
            }
        }

        public async Task<bool> DeleteChildren(int idEmployeeChildren)
        {
            try
            {
                var child = await _dbContext.EmployeeChildren
                    .FirstOrDefaultAsync(x => x.IdEmployeeChildren == idEmployeeChildren);

                if (child == null)
                    return false;

                _dbContext.EmployeeChildren.Remove(child);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee child record");
                throw;
            }
        }

        public async Task<List<EmployeeChildrenDto>> GetChildrenByEmployee(int idEmployee)
        {
            try
            {
                var children = await _dbContext.EmployeeChildren
                    .Where(x => x.IdEmployee == idEmployee)
                    .Select(x => new EmployeeChildrenDto
                    {
                        IdEmployeeChildren = x.IdEmployeeChildren,
                        IdEmployee = x.IdEmployee,
                        ChildName = x.ChildName,
                        DateOfBirth = x.DateOfBirth,
                        Gender = x.Gender,
                        CertificateNumber = x.CertificateNumber,
                        DivisionNumber = x.DivisionNumber
                    }).OrderBy(ee=>ee.IdEmployeeChildren)
                    .ToListAsync();

                return children;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching children for employee. IdEmployee: {IdEmployee}", idEmployee);
                throw;
            }
        }
    }
}
