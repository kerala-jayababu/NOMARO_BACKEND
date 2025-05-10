using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class OvertimeTransactionService : IOvertimeTransactionService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<OvertimeTransactionService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmployeeServices _employeeServices;
        private readonly IConfiguration _configuration;
        private readonly IApprovalWorkflowService _approvalWorkflowService;

        public OvertimeTransactionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<OvertimeTransactionService> logger, IConfiguration configuration, 
                                           IWebHostEnvironment webHostEnvironment, IEmployeeServices employeeServices, IApprovalWorkflowService approvalWorkflowService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _employeeServices = employeeServices;
            _configuration = configuration;
            _approvalWorkflowService = approvalWorkflowService;
        }

        public async Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionList(
       int EmployeeId,
       string? searchText = null,
       DateTime? startDate = null,
       string? dropdownFilter = null)
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
                    var designation = await connection.QueryFirstOrDefaultAsync(designationQuery, new { EmployeeId });
                    if (designation == null)
                        throw new Exception("Employee not found.");

                    // Step 2: Determine which employees to fetch overtime for
                    IEnumerable<int> employeeIds;
                    var requiredDesignationCode = _configuration["Designations:Code"];
                    if (designation.DesignationCode == requiredDesignationCode)
                    {
                        var allEmployees = await connection.QueryAsync<EmployeeHierarchyDto>(allEmployeesQuery);
                        employeeIds = allEmployees.Select(e => e.IdEmployee);
                    }
                    else
                    {
                        var hierarchy = await connection.QueryAsync<EmployeeHierarchyDto>(hierarchyQuery, new { EmployeeId });
                        employeeIds = hierarchy.Select(e => e.IdEmployee);
                    }

                    if (!employeeIds.Any())
                    {
                        _logger.LogInformation("No employees found for the given EmployeeId.");
                        return Enumerable.Empty<OvertimeTransactionDto>();
                    }

                    // Step 3: Fetch overtime transactions
                    var query = new StringBuilder(@"
SELECT 
    ot.IdOvertimeTransaction,
    ot.IdEmployee,
    ot.IdOvertimeType,    
    ot.StartDate,
    ot.StartTime,
    ot.EndDate,
    ot.EndTime,
    ot.DurationInHours,
    ot.ReasonForOvertime,
    ot.Attachment,
    ot.AttachmentDescription,
    ot.ApprovalStatus,
    e.EmployeeCode,    
    CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
    e.IdDepartment,
    e.IdDesignation,
    d.DepartmentName AS Department,
    des.DesignationName AS Designation
FROM OvertimeTransactions ot
INNER JOIN Employees e ON ot.IdEmployee = e.IdEmployee
INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
WHERE ot.IdEmployee IN @EmployeeIds ");

                    var parameters = new DynamicParameters();
                    parameters.Add("EmployeeIds", employeeIds);

                    // Apply search filter
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

                    // Apply start date filter
                    if (startDate.HasValue)
                    {
                        query.Append(" AND ot.StartDate >= @StartDate ");
                        parameters.Add("StartDate", startDate.Value);
                    }

                    if (!string.IsNullOrEmpty(dropdownFilter))
                    {
                        if (dropdownFilter.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                        {
                            query.Append(@"
                    AND (
                        ot.ApprovalStatus = 'SUBMITTED'
                        OR ot.ApprovalStatus = 'INTERIM APPROVED'
                    )");
                        }
                        else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                        {
                           

                            query.Append(@"
                    AND (
                        ot.ApprovalStatus = 'APPROVED'
                        OR ot.ApprovalStatus = 'FINAL APPROVED'
                    )");
                            //query.Append(@"
                            //AND es.ApprovalStatus = 'APPROVED'
                            //AND es.ValidFrom = (
                            //    SELECT MAX(ValidFrom)
                            //    FROM EmployeeSalaryConfig
                            //    WHERE IdEmployee = es.IdEmployee
                            //    AND ApprovalStatus = 'APPROVED'
                            //)");
                        }
                        else
                        {
                            query.Append(" AND ot.ApprovalStatus = @DropdownFilter ");
                            parameters.Add("DropdownFilter", dropdownFilter);

                        }                       
                    }

                    // Apply dropdown filter
                    //if (!string.IsNullOrEmpty(dropdownFilter))
                    //{
                    //    query.Append(" AND ot.ApprovalStatus = @DropdownFilter ");
                    //    parameters.Add("DropdownFilter", dropdownFilter);
                    //}

                    query.Append(" ORDER BY ot.StartDate DESC; ");

                    return await connection.QueryAsync<OvertimeTransactionDto>(query.ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Overtime Transactions for EmployeeId: {EmployeeId}", EmployeeId);
                throw;
            }
        }





        public async Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionsForSelfPortal(int employeeId, DateTime? startDate = null)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    var query = new StringBuilder(@"
SELECT 
    ot.IdOvertimeTransaction,
    ot.IdEmployee,
    ot.IdOvertimeType,    
    ot.StartDate,
    ot.StartTime,
    ot.EndDate,
    ot.EndTime,
    ot.DurationInHours,
    ot.ReasonForOvertime,
    ot.Attachment,
    ot.AttachmentDescription,
    ot.ApprovalStatus,
    e.EmployeeCode,    
    CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
    e.IdDepartment,
    e.IdDesignation,
    d.DepartmentName AS Department,
    des.DesignationName AS Designation
FROM OvertimeTransactions ot
INNER JOIN Employees e ON ot.IdEmployee = e.IdEmployee
INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
WHERE ot.IdEmployee = @EmployeeId ");

                    var parameters = new DynamicParameters();
                    parameters.Add("EmployeeId", employeeId);

                    if (startDate.HasValue)
                    {
                        query.Append(" AND ot.StartDate >= @StartDate ");
                        parameters.Add("StartDate", startDate.Value.Date);
                    }

                    query.Append(" ORDER BY ot.StartDate DESC; ");

                    return await connection.QueryAsync<OvertimeTransactionDto>(query.ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Overtime Transactions for EmployeeId: {EmployeeId}", employeeId);
                throw;
            }
        }



        public async Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id)
        {
            const string query = @"
    SELECT 
        ot.IdOvertimeTransaction,
        ot.IdEmployee,
        ot.IdOvertimeType,
        ot.StartDate,
        ot.StartTime,
        ot.EndDate,
        ot.EndTime,
        ot.DurationInHours,
        ot.ReasonForOvertime,
        ot.Attachment,
        ot.AttachmentDescription,
        ot.ApprovalStatus,
        e.EmployeeCode,
        ot.CreatedOn,
        ot.DayType,
       CONCAT(ec.FirstName, ' ', COALESCE(ec.MiddleName, ''), ' ', ec.LastName) AS CreatedBy,
        CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
        e.IdDepartment,
        e.IdDesignation,
        d.DepartmentName AS Department,
        des.DesignationName AS Designation
    FROM OvertimeTransactions ot
    INNER JOIN Employees e ON ot.IdEmployee = e.IdEmployee
    INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
    INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
    INNER JOIN Employees ec on ot.CreatedBy= ec.IdEmployee
    WHERE ot.IdOvertimeTransaction = @Id;
    ";

            try
            {
                _logger.LogInformation("Fetching Overtime Transaction by ID {Id} using Dapper.", id);

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var transaction = await connection.QueryFirstOrDefaultAsync<OvertimeTransactionDto>(query, new { Id = id });

                    return transaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Overtime Transaction with ID: {Id}.", id);
                throw;
            }
        }


        public async Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transactionDto, int IdEmployee)
        {
            try
            {
                var transactionEntity = _mapper.Map<OvertimeTransactionEntity>(transactionDto);
                string filePath = null;
                string fileNameWithExtension = null;
                if (transactionDto.File != null)
                {
                    string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/OvertimeTransactions");
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }
                    fileNameWithExtension = transactionDto.File.FileName;
                    string uniqueFileName = $"{Guid.NewGuid()}_{transactionDto.IdEmployee}_{transactionDto.File.FileName}";
                    filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await transactionDto.File.CopyToAsync(stream);
                    }
                }
                var holiday = await _dbContext.Holidays
      .FirstOrDefaultAsync(h => h.HolidayDate.Date == transactionDto.StartDate.Date);

                if (holiday != null)
                {
                    transactionEntity.DayType = holiday.HolidayType; // Use the HolidayType from Holidays table
                    transactionEntity.IdOvertimeType =await _dbContext.OvertimeTypes.Where(x => x.OvertimeTypeName == "HOLIDAY").Select(x=>x.IdOvertimeType).FirstOrDefaultAsync();
                }
                else
                {
                    transactionEntity.DayType = "WORKINGDAY"; // Default value if no holiday exists
                    transactionEntity.IdOvertimeType = await _dbContext.OvertimeTypes.Where(x => x.OvertimeTypeName == "Working Day").Select(x => x.IdOvertimeType).FirstOrDefaultAsync();
                }

                transactionEntity.CreatedBy = IdEmployee;
                transactionEntity.CreatedOn = DateTime.Now;
                transactionEntity.Attachment = filePath;
                transactionEntity.ApprovalStatus = "SUBMITTED";
                transactionEntity.AttachmentDescription = fileNameWithExtension;
                await _dbContext.OvertimeTransactions.AddAsync(transactionEntity);
                await _dbContext.SaveChangesAsync();
                int insertedId = transactionEntity.IdOvertimeTransaction;
                var entityCode = _configuration["WorkflowEntityCodes:Overtime"];
                // Step: Call the approval workflow service
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(insertedId, entityCode, transactionEntity.IdEmployee, "SUBMITTED", null);

                return _mapper.Map<OvertimeTransactionDto>(transactionEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding overtime transaction: {@Dto}.", transactionDto);
                return null;
            }
        }

        public async Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transactionDto, int IdEmployee)
        {
            try
            {

                var transaction = await _dbContext.OvertimeTransactions.FirstOrDefaultAsync(x => x.IdOvertimeTransaction == transactionDto.IdOvertimeTransaction);

                if (transaction == null) return null;

                string filePath = transaction.Attachment;

                if (transactionDto.File != null)
                {
                    if (Directory.Exists(transaction.Attachment))
                    {
                        Directory.Delete(transaction.Attachment);
                    }

                    string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/OvertimeTransactions");
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{transactionDto.IdEmployee}_{transactionDto.File.FileName}";
                    filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await transactionDto.File.CopyToAsync(stream);
                    }
                }
                var holiday = await _dbContext.Holidays
    .FirstOrDefaultAsync(h => h.HolidayDate.Date == transactionDto.StartDate.Date);

                if (holiday != null)
                {
                    transaction.DayType = holiday.HolidayType; // Use the HolidayType from Holidays table
                    transaction.IdOvertimeType = await _dbContext.OvertimeTypes.Where(x => x.OvertimeTypeName == "HOLIDAY").Select(x => x.IdOvertimeType).FirstOrDefaultAsync();
                }
                else
                {
                    transaction.DayType = "WORKINGDAY"; // Default value if no holiday exists
                    transaction.IdOvertimeType = await _dbContext.OvertimeTypes.Where(x => x.OvertimeTypeName == "Working Day").Select(x => x.IdOvertimeType).FirstOrDefaultAsync();
                }
                transaction.StartTime = transactionDto.StartTime;
                transaction.EndTime = transactionDto.EndTime;
                transaction.StartDate = transactionDto.StartDate;
                //transaction.IdOvertimeType= transactionDto.IdOvertimeType;               
                transaction.EndDate = transactionDto.EndDate;
                transaction.DurationInHours = transactionDto.DurationInHours;
                transaction.ReasonForOverTime = transactionDto.ReasonForOvertime;
                transaction.AttachmentDescription = transactionDto.AttachmentDescription;
                transaction.ApprovalStatus = transactionDto.ApprovalStatus;
                transaction.Attachment = filePath;

                _dbContext.OvertimeTransactions.Update(transaction);

                await _dbContext.SaveChangesAsync();              
                var entityCode = _configuration["WorkflowEntityCodes:Overtime"];
                // Step: Call the approval workflow service
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(transaction.IdOvertimeTransaction, entityCode, IdEmployee, "SUBMITTED", null);
                return _mapper.Map<OvertimeTransactionDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overtime transaction with ID: {Id}.", transactionDto.IdOvertimeTransaction);
                return null;
            }
        }

        public async Task<decimal> GetOverTimeAmount(int IdEmployee, DateTime OvertimeDate, decimal DurationInHours )
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                string dayType = await _dbContext.Holidays
                    .Where(h => h.HolidayDate.Date.Year == OvertimeDate.Year && 
                        h.HolidayDate.Month == OvertimeDate.Month && h.HolidayDate.Day == OvertimeDate.Day)
                    .Select(h => h.HolidayType)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(dayType))
                {
                    dayType = "WORKINGDAY";
                }

                var config = await _dbContext.EmployeeOvertimeConfig
                    .Where(e => e.IdEmployee ==  IdEmployee && e.DayType == dayType)
                    .Select(e => new { e.StandardRate, e.DayRate })
                    .FirstOrDefaultAsync();

                decimal standardRate = config?.StandardRate ?? 0;
                decimal dayRate = config?.DayRate ?? 0;

                decimal overtimeAmount = standardRate * dayRate * DurationInHours;

                return Math.Round(overtimeAmount, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching  Leave passage.");
                throw new Exception("An error occurred while fetching maternity leave passage. Please try again later.");

            }
        }
    }
}
