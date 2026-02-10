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
       int EmployeeId,string? searchText = null,DateTime? startDate = null,string? dropdownFilter = null)
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
                    des.DesignationName AS Designation,
                    sm.SalaryMonthText,
	                SalaryAccountedAmount
                FROM OvertimeTransactions ot
                INNER JOIN Employees e ON ot.IdEmployee = e.IdEmployee
                INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
                INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
                LEFT OUTER JOIN SalaryMonths sm on ot.IdSalaryMonthAccounted = sm.idSalaryMonth
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
                        INNER JOIN Employees ec on ot.CreatedBy = ec.IdEmployee
                        WHERE ot.IdOvertimeTransaction = @Id;
                        ";

                                    const string configQuery = @"
                        SELECT 
                            IdEmployeeOvertimeConfig,
                            IdEmployee,
                            DayType,
                            StandardRate,
                            DayRate
                        FROM EmployeeOvertimeConfig
                        WHERE IdEmployee = @IdEmployee;
                        ";

            try
            {
                _logger.LogInformation("Fetching Overtime Transaction by ID {Id} using Dapper.", id);

                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    // 1. Get overtime transaction
                    var transaction = await connection.QueryFirstOrDefaultAsync<OvertimeTransactionDto>(query, new { Id = id });

                    if (transaction == null)
                        return null;

                    // 2. Get OT Amount
                    var res = await GetOverTimeAmount(transaction.IdEmployee, transaction.StartDate, transaction.DurationInHours);
                    transaction.OTAmount = res;

                    // 3. Get configs
                    var configs = await connection.QueryAsync<EmployeeOvertimeConfigDto>(configQuery, new { IdEmployee = transaction.IdEmployee });
                    transaction.OvertimeConfigs = configs.ToList();

                    return transaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Overtime Transaction with ID: {Id}.", id);
                throw;
            }
        }

        public async Task<bool> IsOverTimeTransactionAllowed(int IdEmployee)
        {
            var otStatus = await _dbContext.Employees
               .Where(em => em.IdEmployee == IdEmployee)
               .Select(em => em.OverTimeAllowedStatus)
               .FirstOrDefaultAsync();

                    // If employee not found or status is null → not allowed
                if (!string.IsNullOrWhiteSpace(otStatus))
                {
                    if (otStatus.ToUpper() == "TRUE")
                        return true;
                }
            return false;
        }


        public async Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transactionDto, int idEmployee)
        {
            try
            {

                var result = await _dbContext.Employees
                       .Where(e => e.IdEmployee == transactionDto.IdEmployee)
                       .Select(e => e.OverTimeAllowedStatus)
                       .FirstOrDefaultAsync();

                if(result == "false")
                {
                    throw new ArgumentException("Overtime Transaction is not permitted for you");

                }
                //To check whether more than 2 OT is already submitted for the same period
                var currentOTCount = await _dbContext.OvertimeTransactions
                    .Where(e =>
                        e.IdEmployee == transactionDto.IdEmployee &&
                        transactionDto.StartDate <= e.EndDate &&
                        transactionDto.EndDate >= e.StartDate
                    ).CountAsync();
                if(currentOTCount >2)
                {
                    throw new ArgumentException("There are Overtime Transactions already submitted for these given period");
                }

                // 🔹 Map DTO to entity
                var transactionEntity = _mapper.Map<OvertimeTransactionEntity>(transactionDto);

                // 🔹 Attach file if uploaded
                (string filePath, string fileName) = await SaveAttachmentAsync(transactionDto);
                transactionEntity.Attachment = filePath;
                transactionEntity.AttachmentDescription = fileName;

                // 🔹 Determine Day Type (Holiday or Working Day)
                await SetDayTypeAsync(transactionEntity);

                // 🔹 Audit fields
                transactionEntity.CreatedBy = idEmployee;
                transactionEntity.CreatedOn = DateTime.Now;
                transactionEntity.ApprovalStatus = "SUBMITTED";

                // 🔹 Save transaction
                await _dbContext.OvertimeTransactions.AddAsync(transactionEntity);
                await _dbContext.SaveChangesAsync();
                int insertedId = transactionEntity.IdOvertimeTransaction;

                // 🔹 Start Workflow
                await HandleApprovalWorkflow(transactionEntity, idEmployee, insertedId,false);

                return _mapper.Map<OvertimeTransactionDto>(transactionEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding overtime transaction: {@Dto}.", transactionDto);
                return null;
            }
        }

        #region Helpers

        private async Task<(string FilePath, string FileName)> SaveAttachmentAsync(OvertimeTransactionDto dto)
        {
            if (dto.File == null) return (null, null);

            string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/OvertimeTransactions");
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
            }

            string fileName = dto.File.FileName;
            string uniqueFileName = $"{Guid.NewGuid()}_{dto.IdEmployee}_{fileName}";
            string filePath = Path.Combine(uploadFolderPath, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await dto.File.CopyToAsync(stream);

            return (filePath, fileName);
        }

        private async Task SetDayTypeAsync(OvertimeTransactionEntity entity)
        {
            var holiday = await _dbContext.Holidays
                .FirstOrDefaultAsync(h => h.HolidayDate.Date == entity.StartDate.Date);

            if (holiday != null)
            {
                entity.DayType = holiday.HolidayType;
                entity.IdOvertimeType = await _dbContext.OvertimeTypes
                    .Where(x => x.OvertimeTypeName == "HOLIDAY")
                    .Select(x => x.IdOvertimeType)
                    .FirstOrDefaultAsync();
            }
            else
            {
                entity.DayType = "WORKINGDAY";
                entity.IdOvertimeType = await _dbContext.OvertimeTypes
                    .Where(x => x.OvertimeTypeName == "Working Day")
                    .Select(x => x.IdOvertimeType)
                    .FirstOrDefaultAsync();
            }
        }

        private async Task HandleApprovalWorkflow(OvertimeTransactionEntity entity, int idEmployee, int insertedId, bool isUpdate = false)
        {
            var workflowConfig = await _dbContext.WorkFlowConfig
                .Where(x => x.EntityCode == "OVERTIME")
                .ToListAsync();

            if (!workflowConfig.Any()) return;

            string entityCode = _configuration["WorkflowEntityCodes:Overtime"];
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(x => x.IdEmployee == entity.IdEmployee);

            bool isSelfReporting = employee != null && employee.ReportingTo == idEmployee;

            // 🔹 Find current cycle index if update
            int cycleIndex = 1;
            if (isUpdate)
            {
                var lastCycle = await _dbContext.ApprovalWorkFlowAllocations
                    .Where(a => a.EntityCode == entityCode && a.EntityTablePrimaryKeyID == insertedId)
                    .OrderByDescending(a => a.CycleIndex)
                    .FirstOrDefaultAsync();

                cycleIndex = (lastCycle?.CycleIndex ?? 0) + 1;
            }

            if (isSelfReporting)
            {
                var firstLevel = await _dbContext.WorkFlowConfigDetails
                    .FirstOrDefaultAsync(w =>
                        w.IdWorkFlowConfig == workflowConfig.First().IdWorkFlowConfig &&
                        w.LevelNumber == 1);

                if (firstLevel != null)
                {
                    var targetEmployees = await GetTargetEmployeesForOT(firstLevel, entity.IdEmployee);
                    var targetEmployeeIds = string.Join(",", targetEmployees);

                    var autoApprovedRecord = new ApprovalWorkFlowAllocation
                    {
                        IdWorkFlowConfig = workflowConfig.First().IdWorkFlowConfig,
                        EntityCode = entityCode,
                        EntityTablePrimaryKeyID = insertedId,
                        CycleIndex = cycleIndex,   // ✅ dynamic cycle index
                        LevelNumber = 1,
                        SourceIdEmployee = entity.IdEmployee,
                        TargetIdEmployee = targetEmployeeIds,
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(autoApprovedRecord);
                    await _dbContext.SaveChangesAsync();

                    await _approvalWorkflowService.InitiateApprovalWorkflow(
                        insertedId, entityCode, idEmployee, firstLevel.ApprovalStatusName, null, null);
                }
            }
            else
            {
                await _approvalWorkflowService.InitiateApprovalWorkflow(
                    insertedId, entityCode, entity.IdEmployee, "SUBMITTED", null, null);
            }
        }


        #endregion

        private async Task<List<int>> GetTargetEmployeesForOT(WorkFlowConfigDetails workflowConfigDetails, int loggedInEmployeeId)
        {
            if (workflowConfigDetails == null)
                return new List<int> { 0 };

            List<int> result = new List<int>();

            if (workflowConfigDetails.ApprovalAuthorityType == "ROLE")
            {
                var designationId = workflowConfigDetails.ApprovalAuthorityID;
                if (designationId.HasValue)
                {
                    result = await _dbContext.Employees
                        .Where(e => e.IdDesignation == designationId.Value && e.IdEmployee.HasValue)
                        .Select(e => e.IdEmployee.Value)
                        .ToListAsync();
                }
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "REPOFFICER"
                     && workflowConfigDetails.ApprovalAuthorityID.HasValue)
            {
                result = await _dbContext.Employees
                    .Where(e => e.IdEmployee == loggedInEmployeeId && e.ReportingTo.HasValue)
                    .Select(e => e.ReportingTo.Value)
                    .ToListAsync();
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "EMPLOYEE")
            {
                if (workflowConfigDetails.ApprovalAuthorityID.HasValue)
                {
                    result = await _dbContext.Employees
                        .Where(e => e.IdEmployee == workflowConfigDetails.ApprovalAuthorityID.Value && e.IdEmployee.HasValue)
                        .Select(e => e.IdEmployee.Value)
                        .ToListAsync();
                }
            }

            // If result is null or empty, append 0
            if (result == null || !result.Any())
                result = new List<int> { 0 };

            return result;
        }

        

        public async Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transactionDto, int idEmployee)
        {
            try
            {
                var transactionEntity = await _dbContext.OvertimeTransactions
                    .FirstOrDefaultAsync(x => x.IdOvertimeTransaction == transactionDto.IdOvertimeTransaction);

                if (transactionEntity == null) return null;

                // 🔹 Update attachment if new file uploaded
                if (transactionDto.File != null)
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(transactionEntity.Attachment) && File.Exists(transactionEntity.Attachment))
                    {
                        File.Delete(transactionEntity.Attachment);
                    }

                    (string filePath, string fileName) = await SaveAttachmentAsync(transactionDto);
                    transactionEntity.Attachment = filePath;
                    transactionEntity.AttachmentDescription = fileName;
                }

                // 🔹 Update Day Type (holiday vs working day)
                await SetDayTypeAsync(transactionEntity);

                // 🔹 Update fields
                transactionEntity.StartTime = transactionDto.StartTime;
                transactionEntity.EndTime = transactionDto.EndTime;
                transactionEntity.StartDate = transactionDto.StartDate;
                transactionEntity.EndDate = transactionDto.EndDate;
                transactionEntity.DurationInHours = transactionDto.DurationInHours;
                transactionEntity.ReasonForOverTime = transactionDto.ReasonForOvertime;
                transactionEntity.ApprovalStatus = "SUBMITTED"; // always reset to submitted when updated

                _dbContext.OvertimeTransactions.Update(transactionEntity);
                await _dbContext.SaveChangesAsync();

                // 🔹 Workflow logic (same as Add)
                await HandleApprovalWorkflow(transactionEntity, idEmployee, transactionEntity.IdOvertimeTransaction,true);

                return _mapper.Map<OvertimeTransactionDto>(transactionEntity);
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
        public async Task<IEnumerable<OvertimeTransactionFullDto>> GetOvertimeTransactionsFullDetails(int EmployeeId)
        {
            const string entityCode = "OVERTIME";

            const string deptAndDesignationQuery = @"
            SELECT TOP 1
                d.DepartmentCode,
                des.DesignationCode
            FROM Employees e
            INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            WHERE e.IdEmployee = @EmployeeId;
            ";

            const string allEmployeesIdsQuery = @"SELECT IdEmployee FROM Employees;";

                        // ✅ Robust: avoid failing due to spaces/case mismatch
            const string wfConfigQuery = @"
            SELECT TOP 1 IdWorkFlowConfig
            FROM WorkFlowConfig
            WHERE UPPER(LTRIM(RTRIM(EntityCode))) = UPPER(LTRIM(RTRIM(@EntityCode)));
            ";

                        const string overtimeQuery = @"
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
                ot.DayType,
                CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
                e.IdDepartment,
                e.IdDesignation,
                d.DepartmentName AS Department,
                ot.IdSalaryMonthAccounted,
                ot.SalaryAccountedAmount,
                des.DesignationName AS Designation
            FROM OvertimeTransactions ot
            INNER JOIN Employees e ON ot.IdEmployee = e.IdEmployee
            INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            WHERE ot.IdEmployee IN @EmployeeIds
            ORDER BY ot.StartDate ASC;
            ";

            // ✅ Approvals from WCD (master) + optional overlay from AWA
            const string approvalsQuery = @"
                ;WITH wcd AS (
                    SELECT
                        d.IdWorkFlowConfigDetail,
                        d.IdWorkFlowConfig,
                        d.LevelNumber,
                        d.ApprovalAuthorityType,
                        d.ApprovalAuthorityID,
                        d.ApprovalStatusName
                    FROM WorkFlowConfigDetails d
                    WHERE d.IdWorkFlowConfig = @IdWorkFlowConfig
                ),
                ot AS (
                    SELECT
                        ot.IdOvertimeTransaction,
                        ot.IdEmployee,
                        e.ReportingTo,
                        e.IdDepartment
                    FROM OvertimeTransactions ot
                    INNER JOIN Employees e ON e.IdEmployee = ot.IdEmployee
                    WHERE ot.IdOvertimeTransaction IN @OvertimeIds
                ),
                expected AS (
                    SELECT
                        wcd.IdWorkFlowConfigDetail,
                        ot.IdOvertimeTransaction,
                        wcd.IdWorkFlowConfig,
                        wcd.LevelNumber,
                        wcd.ApprovalAuthorityType,
                        wcd.ApprovalAuthorityID,
                        wcd.ApprovalStatusName,

                        -- REPOFFICER -> reporting officer id
                        CASE
                            WHEN wcd.ApprovalAuthorityType = 'REPOFFICER' THEN ot.ReportingTo
                            ELSE NULL
                        END AS RepoOfficerEmployeeId,

                        -- ROLE -> all employees with designation = ApprovalAuthorityID
                        CASE
                            WHEN wcd.ApprovalAuthorityType = 'ROLE' THEN (
                                SELECT STRING_AGG(
                                    CONCAT(emp.FirstName, ' ', COALESCE(emp.MiddleName, ''), ' ', emp.LastName),
                                    ', '
                                )
                                FROM Employees emp
                                WHERE emp.IdDesignation = wcd.ApprovalAuthorityID
                            )
                            ELSE NULL
                        END AS RoleApproverNames
                    FROM ot
                    CROSS JOIN wcd
                ),
                awa_pick AS (
                    SELECT
                        awa.EntityTablePrimaryKeyID,
                        awa.LevelNumber,
                        awa.ActionedBy,
                        awa.ActionDate,
                        ROW_NUMBER() OVER (
                            PARTITION BY awa.EntityTablePrimaryKeyID, awa.LevelNumber
                            ORDER BY awa.ActionDate DESC, awa.IdApprovalWorkFlow DESC
                        ) AS rn
                    FROM ApprovalWorkFlowAllocations awa
                    WHERE awa.EntityTablePrimaryKeyID IN @OvertimeIds
                )
                SELECT
                    ex.IdWorkFlowConfigDetail,
                    ex.IdOvertimeTransaction,
                    ex.IdWorkFlowConfig,
                    ex.LevelNumber,
                    ex.ApprovalAuthorityType,
                    ex.ApprovalAuthorityID,
                    ex.ApprovalStatusName,

                    -- ApprovalAuthorityName
                    CASE
                        WHEN ex.ApprovalAuthorityType = 'REPOFFICER'
                            THEN CONCAT(tgt.FirstName, ' ', COALESCE(tgt.MiddleName, ''), ' ', tgt.LastName)
                        WHEN ex.ApprovalAuthorityType = 'ROLE'
                            THEN ex.RoleApproverNames
                        ELSE NULL
                    END AS ApprovalAuthorityName,

                  -- ApprovalAuthorityEmployeeIDs (return IdEmployee(s))
CASE
    WHEN ex.ApprovalAuthorityType = 'REPOFFICER'
        THEN CAST(ex.RepoOfficerEmployeeId AS VARCHAR(20))

    WHEN ex.ApprovalAuthorityType = 'ROLE'
        THEN (
            SELECT STRING_AGG(CAST(emp.IdEmployee AS VARCHAR(20)), ',')
            FROM Employees emp
            WHERE emp.IdDesignation = ex.ApprovalAuthorityID
        )

    ELSE NULL
END AS ApprovalAuthorityIdEmployees,
                   

                    ap.ActionDate,

                    -- ActionedBy fields
                    ap.ActionedBy AS ActionedById,
                    CASE
                        WHEN ap.ActionedBy IS NULL THEN NULL
                        ELSE CONCAT(act.FirstName, ' ', COALESCE(act.MiddleName, ''), ' ', act.LastName)
                    END AS ActionedByName

                FROM expected ex
                LEFT JOIN awa_pick ap
                    ON ap.EntityTablePrimaryKeyID = ex.IdOvertimeTransaction
                   AND ap.LevelNumber = ex.LevelNumber
                   AND ap.rn = 1
                LEFT JOIN Employees tgt ON tgt.IdEmployee = ex.RepoOfficerEmployeeId
                LEFT JOIN Employees act ON act.IdEmployee = ap.ActionedBy
                ORDER BY ex.IdOvertimeTransaction, ex.LevelNumber;
                ";

            using var connection = _dbContext.Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();

            // 1) Validate login employee
            var meta = await connection.QueryFirstOrDefaultAsync(deptAndDesignationQuery, new { EmployeeId });
            if (meta == null) throw new Exception("Employee not found.");

            var requiredDesignationCode = _configuration["Designations:Code"];
            bool isHrd = string.Equals((string?)meta.DepartmentCode, "HRD", StringComparison.OrdinalIgnoreCase);
            bool isAdminDesignation = string.Equals((string?)meta.DesignationCode, requiredDesignationCode, StringComparison.OrdinalIgnoreCase);

            // 2) Employee scope
            IEnumerable<int> employeeIds;
            if (isHrd || isAdminDesignation)
            {
                employeeIds = await connection.QueryAsync<int>(allEmployeesIdsQuery);
            }
            else
            {
                var hierarchy = await GetEmployeeHierarchy(EmployeeId);
                hierarchy.Add(EmployeeId);
                employeeIds = hierarchy.Distinct();
            }

            if (!employeeIds.Any())
                return Enumerable.Empty<OvertimeTransactionFullDto>();

            // 3) Overtime list
            var overtimeList = (await connection.QueryAsync<OvertimeTransactionFullDto>(
                overtimeQuery,
                new { EmployeeIds = employeeIds }
            )).ToList();

            if (!overtimeList.Any())
                return overtimeList;

            // 4) Get workflow config id dynamically (NO hardcode)
            var idWorkFlowConfig = await connection.QueryFirstOrDefaultAsync<int?>(
                wfConfigQuery,
                new { EntityCode = entityCode }
            );

            if (idWorkFlowConfig == null)
            {
                foreach (var ot in overtimeList) ot.ApprovalCycles = new List<ApprovalCycleDto>();
                return overtimeList;
            }

            // 5) Nested approvals (WCD always drives rows)
            var overtimeIds = overtimeList.Select(x => x.IdOvertimeTransaction).Distinct().ToList();

            var approvals = (await connection.QueryAsync<ApprovalCycleDto>(
                approvalsQuery,
                new
                {
                    IdWorkFlowConfig = idWorkFlowConfig.Value,
                    OvertimeIds = overtimeIds
                }
            )).ToList();

            var map = approvals
                .GroupBy(a => a.IdOvertimeTransaction)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var ot in overtimeList)
            {
                ot.ApprovalCycles = map.TryGetValue(ot.IdOvertimeTransaction, out var cycles)
                    ? cycles
                    : new List<ApprovalCycleDto>();
            }

            return overtimeList;
        }

        // Helper DTO only for workflow map query
        private class OvertimeWorkflowPair
        {
            public int IdOvertimeTransaction { get; set; }
            public int IdWorkFlowConfig { get; set; }
        }

        private async Task<List<int>> GetEmployeeHierarchy(int managerId)
        {
            var hierarchyIds = new List<int>();
            var allEmployees = await _dbContext.Employees.ToListAsync();

            // Get all employees reporting to the manager (recursively)
            GetDirectReports(managerId, allEmployees, hierarchyIds);

            return hierarchyIds;
        }

        private void GetDirectReports(int managerId, List<Employee> allEmployees, List<int> result)
        {
            // Get direct reports of the manager
            var directReports = allEmployees
                .Where(e => e.ReportingTo == managerId)
                .ToList();

            foreach (var employee in directReports)
            {
                if (employee.IdEmployee.HasValue && !result.Contains(employee.IdEmployee.Value))
                {
                    result.Add(employee.IdEmployee.Value);
                    // Recursively get their reports
                    GetDirectReports(employee.IdEmployee.Value, allEmployees, result);
                }
            }
        }


    }
}
