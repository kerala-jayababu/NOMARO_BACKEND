using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Nomaro.API.Services.Implimentation
{
    public class EmployeeSalaryConfigService : IEmployeeSalaryConfigService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeSalaryConfigService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IAuditService _auditService;

        public EmployeeSalaryConfigService(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<EmployeeSalaryConfigService> logger,
            IConfiguration configuration,
            IApprovalWorkflowService approvalWorkflowService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _approvalWorkflowService = approvalWorkflowService;
            _auditService = auditService;
        }

        #region EmployeeSalaryConfig
        public async Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, string? dropdownFilter = null, DateTime? date = null)
        {
            var query = new StringBuilder(@"
  SELECT 
    esc.IdEmployeeSalaryConfig,
    esc.ValidFrom,
    esc.ValidTo,
    esc.IdSalaryTemplate,
    esc.ActiveStatus,
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
    esc.TotalEarnings,
    esc.TotalDeductions,
    esc.NetSalary,
    esc.ApprovalStatus
FROM Employees e
LEFT JOIN EmployeeSalaryConfig esc 
    ON esc.IdEmployee = e.IdEmployee 
	LEFT JOIN Departments d ON e.IdDepartment = d.IdDepartment
LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
    WHERE 1=1 ");

            var parameters = new DynamicParameters();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchText))
            {
                query.Append(@"
        AND (
            e.EmployeeCode LIKE @SearchText
            OR CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) LIKE @SearchText
            OR des.DesignationName LIKE @SearchText
            OR d.DepartmentName LIKE @SearchText
        ) ");
                parameters.Add("SearchText", $"%{searchText}%");
            }

            if (string.IsNullOrEmpty(dropdownFilter))
            {
                query.Append(@"
        AND esc.IdEmployeeSalaryConfig IN (
            SELECT IdEmployeeSalaryConfig FROM vw_LatestEmployeeSalaryConfig
        )
        AND e.CurrentStatus = 'Working'");
            }
            else if (dropdownFilter.Equals("Show All", StringComparison.OrdinalIgnoreCase))
            {
                // No extra filter - show all records
            }
            else if (dropdownFilter.Equals("Not Configured", StringComparison.OrdinalIgnoreCase))
            {
                // Select employees without any salary configuration
                query.Clear();
                query.Append(@"
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
            e.CurrentStatus
        FROM Employees e
        LEFT JOIN EmployeeSalaryConfig esc ON esc.IdEmployee = e.IdEmployee
        LEFT JOIN Departments d ON e.IdDepartment = d.IdDepartment
        LEFT JOIN Designations des ON e.IdDesignation = des.IdDesignation
        WHERE esc.IdEmployee IS NULL");
            }
            else if (dropdownFilter.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
            {
                query.Append(" AND esc.ApprovalStatus IN ('SUBMITTED', 'INTERIM APPROVED') ");
            }
            else if (dropdownFilter.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                query.Append(" AND esc.ApprovalStatus = 'REJECTED' ");
            }
            else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                // Include only the record with MAX(ValidFrom) for APPROVED status
                query.Append(@"
            AND esc.ApprovalStatus = 'APPROVED'
            AND esc.ValidFrom = (
                SELECT MAX(ValidFrom)
                FROM EmployeeSalaryConfig
                WHERE IdEmployee = esc.IdEmployee
                AND ApprovalStatus = 'APPROVED'
            )");
            }



            if (date.HasValue)
            {
                query.Append(@"
        AND esc.ValidFrom >= @Date ");
                parameters.Add("Date", date.Value.Date); // .Date ensures time portion is ignored
            }


            query.Append(" ORDER BY e.FirstName, e.LastName;");
            var notConfiguredQuery = new StringBuilder(@"
    SELECT COUNT(DISTINCT e.IdEmployee)
    FROM Employees e
    LEFT JOIN EmployeeSalaryConfig esc ON esc.IdEmployee = e.IdEmployee
    WHERE esc.IdEmployee IS NULL
      AND e.CurrentStatus = 'Working'
");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var configs = await connection.QueryAsync<EmployeeSalaryConfigDto>(query.ToString(), parameters);
                    var configList = configs.ToList();
                    var notConfiguredCount = await connection.ExecuteScalarAsync<int>(notConfiguredQuery.ToString());
                    if (configList.Any())
                        configList[0].notConfiguredCount = notConfiguredCount;
                    return configList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Salary Configurations.");
                throw new Exception("An error occurred while fetching configurations. Please try again later.");
            }
        }


        public async Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigsSp(string? searchText = null, string? dropdownFilter = null, bool isLatest = true)
        {
            // Map dropdownFilter to stored procedure @Status
            string statusParam = "ALL";
            if (!string.IsNullOrWhiteSpace(dropdownFilter))
            {
                if (dropdownFilter.Equals("Show All", StringComparison.OrdinalIgnoreCase)) statusParam = "ALL";
                else if (dropdownFilter.Equals("Not Configured", StringComparison.OrdinalIgnoreCase)) statusParam = "NOT CONFIGURED";
                else if (dropdownFilter.Equals("Submitted", StringComparison.OrdinalIgnoreCase)) statusParam = "SUBMITTED";
                else if (dropdownFilter.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) statusParam = "REJECTED";
                else if (dropdownFilter.Equals("APPROVED", StringComparison.OrdinalIgnoreCase)) statusParam = "APPROVED";
                else statusParam = dropdownFilter;
            }

            var parameters = new DynamicParameters();
            parameters.Add("QueryType", isLatest ? "LATEST" : "ALL");
            parameters.Add("SearchString", string.IsNullOrWhiteSpace(searchText) ? null : searchText);
            parameters.Add("Status", statusParam);

            var procName = "[dbo].[GetLatestSalaryConfigForListing]";

//            var notConfiguredQuery = new StringBuilder(@"
//    SELECT COUNT(DISTINCT e.IdEmployee)
//    FROM Employees e
//    LEFT JOIN EmployeeSalaryConfig esc ON esc.IdEmployee = e.IdEmployee
//    WHERE esc.IdEmployee IS NULL
//      AND e.CurrentStatus = 'Working'
//");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var configs = await connection.QueryAsync<EmployeeSalaryConfigDto>(procName, parameters, commandType: System.Data.CommandType.StoredProcedure);
                    var configList = configs.ToList();
                    //var notConfiguredCount = await connection.ExecuteScalarAsync<int>(notConfiguredQuery.ToString());
                    //if (configList.Any())
                    //    configList[0].notConfiguredCount = notConfiguredCount;
                    return configList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Salary Configurations via stored procedure.");
                throw new Exception("An error occurred while fetching configurations. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> GetConfigById(int id)
        {
            try
            {
                var config = await (from c in _dbContext.EmployeeSalaryConfig
                                    join emp in _dbContext.Employees
                                    on c.CreatedBy equals emp.IdEmployee into empGroup
                                    from emp in empGroup.DefaultIfEmpty()
                                    where c.IdEmployeeSalaryConfig == id
                                    select new EmployeeSalaryConfigDto
                                    {
                                        IdEmployeeSalaryConfig = c.IdEmployeeSalaryConfig,
                                        CreatedBy = c.CreatedBy ?? 0,
                                        CreatedByValue = emp != null ? emp.FirstName + " " + emp.MiddleName + " " + emp.LastName : null,
                                        IdEmployee = c.IdEmployee,
                                        ValidFrom = c.ValidFrom,
                                        ValidTo = c.ValidTo,
                                        IdSalaryTemplate = c.IdSalaryTemplate,
                                        ApprovalStatus = c.ApprovalStatus,
                                        ActiveStatus = c.ActiveStatus,
                                        CreatedOn = c.CreatedOn ?? DateTime.MinValue,
                                        ModifiedBy = c.ModifiedBy,
                                        ModifiedOn = c.ModifiedOn,
                                        ApprovedBy = c.ApprovedBy,
                                        ApprovedOn = c.ApprovedOn,
                                        ApprovedDate = c.ApprovedOn,
                                        RevisionReason = c.RevisionReason,
                                        TotalEarnings = c.TotalEarnings,
                                        TotalDeductions = c.TotalDeductions,
                                        NetSalary = c.NetSalary,
                                        CTCAnnual = c.CTCAnnual,
                                        CTCMonthly = c.CTCMonthly,
                                        GrossMonthly = c.GrossMonthly,
                                        TotalEmployerContribution = c.TotalEmployerContribution
                                    })
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();

                if (config != null)
                {
                    config.EmployeeSalaryConfigDetails = await GetConfigDetails(id);
                }

                if (config == null)
                {
                    return null; // Return null if not found
                }

                // Fetch additional employee details using raw SQL query
                var query = @"
            SELECT 
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
                e.OverTimeAllowedStatus
            FROM EmployeeSalaryConfig esc
            INNER JOIN Employees e ON esc.IdEmployee = e.IdEmployee
            INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
            INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
            WHERE esc.IdEmployeeSalaryConfig = @ConfigId";

                // Do not dispose this connection â€” it is owned by the DbContext. Disposing it breaks later EF queries
                // on the same context (e.g. GetLatestApprovedConfigByEmployeeId â†’ BuildSalaryConfigApprovalStepsAsync).
                var connection = _dbContext.Database.GetDbConnection();
                var wasClosed = connection.State == System.Data.ConnectionState.Closed;
                if (wasClosed)
                    await connection.OpenAsync();
                try
                {
                    var employeeInfo = await connection.QueryFirstOrDefaultAsync<EmployeeSalaryConfigDto>(query, new { ConfigId = id });

                    if (employeeInfo != null)
                    {
                        config.EmployeeCode = employeeInfo.EmployeeCode;
                        config.EmployeeName = employeeInfo.EmployeeName;
                        config.IdDesignation = employeeInfo.IdDesignation;
                        config.DesignationName = employeeInfo.DesignationName;
                        config.IdDepartment = employeeInfo.IdDepartment;
                        config.DepartmentName = employeeInfo.DepartmentName;
                        config.JoiningDate = employeeInfo.JoiningDate;
                        config.Gender = employeeInfo.Gender;
                        config.EmailID = employeeInfo.EmailID;
                        config.PhoneNumber1 = employeeInfo.PhoneNumber1;
                        config.PhoneNumber2 = employeeInfo.PhoneNumber2;
                        config.CurrentStatus = employeeInfo.CurrentStatus;
                        config.OverTimeAllowedStatus = employeeInfo.OverTimeAllowedStatus;
                    }
                }
                finally
                {
                    if (wasClosed && connection.State == System.Data.ConnectionState.Open)
                        await connection.CloseAsync();
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Salary Configuration with ID {Id}.", id);
                return null;
            }
        }

        public async Task<LatestApprovedEmployeeSalaryConfigResponseDto?> GetLatestApprovedConfigByEmployeeId(int idEmployee)
        {
            try
            {
                var approvedStatuses = new[] { "APPROVED", "FINAL APPROVED" };

                var configId = await _dbContext.EmployeeSalaryConfig
                    .AsNoTracking()
                    .Where(c => c.IdEmployee == idEmployee && c.ApprovalStatus != null && approvedStatuses.Contains(c.ApprovalStatus))
                    .OrderByDescending(c => c.ValidFrom)
                    .ThenByDescending(c => c.IdEmployeeSalaryConfig)
                    .Select(c => c.IdEmployeeSalaryConfig)
                    .FirstOrDefaultAsync();

                if (configId == 0)
                {
                    return null;
                }

                var config = await GetConfigById(configId);
                if (config == null)
                {
                    return null;
                }

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"] ?? "EMPLSALCONFIG";
                var steps = await BuildSalaryConfigApprovalTimelineAsync(configId, entityCode, config);

                return new LatestApprovedEmployeeSalaryConfigResponseDto
                {
                    Config = config,
                    ApprovalWorkflowSteps = steps
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest approved Employee Salary Configuration for employee {IdEmployee}.", idEmployee);
                return null;
            }
        }

        private async Task<List<SalaryConfigApprovalTimelineStepDto>> BuildSalaryConfigApprovalTimelineAsync(
            int configId,
            string entityCode,
            EmployeeSalaryConfigDto config)
        {
            var timeline = new List<SalaryConfigApprovalTimelineStepDto>();

            var allocations = await _dbContext.ApprovalWorkFlowAllocations
                .AsNoTracking()
                .Where(a => a.EntityTablePrimaryKeyID == configId && a.EntityCode == entityCode)
                .ToListAsync();

            var rowList = new List<(ApprovalWorkFlowAllocation awa, WorkFlowConfigDetails? wfd)>();
            if (allocations.Count > 0)
            {
                var maxCycle = allocations.Max(a => a.CycleIndex);
                var queried = await (
                    from awa in _dbContext.ApprovalWorkFlowAllocations.AsNoTracking()
                    join wfd in _dbContext.WorkFlowConfigDetails.AsNoTracking()
                        on new { awa.IdWorkFlowConfig, awa.LevelNumber } equals new { wfd.IdWorkFlowConfig, wfd.LevelNumber } into wfdGroup
                    from wfd in wfdGroup.DefaultIfEmpty()
                    where awa.EntityTablePrimaryKeyID == configId
                        && awa.EntityCode == entityCode
                        && awa.CycleIndex == maxCycle
                    orderby awa.LevelNumber
                    select new { awa, wfd }
                ).ToListAsync();
                rowList = queried.Select(x => (x.awa, x.wfd)).ToList();
            }

            var employeeIds = rowList
                .SelectMany(r => new int?[] { r.awa.ActionedBy, r.awa.SourceIdEmployee })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Append(config.CreatedBy)
                .Distinct()
                .ToList();

            var employeeLookup = await BuildSalaryConfigEmployeeLookupAsync(employeeIds);

            if (config.CreatedBy > 0 && config.CreatedOn != default)
            {
                employeeLookup.TryGetValue(config.CreatedBy, out var creator);
                timeline.Add(new SalaryConfigApprovalTimelineStepDto
                {
                    EventKind = "CREATED",
                    DisplayLabel = "Configuration created",
                    EventDate = config.CreatedOn,
                    ActorEmployeeId = config.CreatedBy,
                    ActorName = !string.IsNullOrWhiteSpace(config.CreatedByValue)
                        ? config.CreatedByValue.Trim()
                        : creator?.Name,
                    ActorDesignationName = creator?.DesignationName,
                    WorkflowLevelNumber = null,
                    IdApprovalWorkFlow = null
                });
            }

            if (rowList.Count == 0)
            {
                return timeline;
            }

            var firstLevel = rowList[0].awa;
            employeeLookup.TryGetValue(firstLevel.SourceIdEmployee, out var submitter);
            timeline.Add(new SalaryConfigApprovalTimelineStepDto
            {
                EventKind = "SUBMITTED",
                DisplayLabel = "Submitted for approval",
                EventDate = firstLevel.SentDate,
                ActorEmployeeId = firstLevel.SourceIdEmployee,
                ActorName = submitter?.Name,
                ActorDesignationName = submitter?.DesignationName,
                WorkflowLevelNumber = null,
                IdApprovalWorkFlow = firstLevel.IdApprovalWorkFlow
            });

            foreach (var (awa, wfd) in rowList)
            {
                if (!awa.ActionedBy.HasValue || !awa.ActionDate.HasValue)
                {
                    continue;
                }

                employeeLookup.TryGetValue(awa.ActionedBy.Value, out var approver);
                var levelLabel = wfd?.ApprovalStatusName;
                var displayLabel = !string.IsNullOrWhiteSpace(awa.ActionStatus)
                    ? awa.ActionStatus
                    : (!string.IsNullOrWhiteSpace(levelLabel) ? levelLabel : "Approved");

                timeline.Add(new SalaryConfigApprovalTimelineStepDto
                {
                    EventKind = "APPROVED",
                    DisplayLabel = displayLabel,
                    EventDate = awa.ActionDate.Value,
                    ActorEmployeeId = awa.ActionedBy,
                    ActorName = approver?.Name,
                    ActorDesignationName = approver?.DesignationName,
                    WorkflowLevelNumber = awa.LevelNumber,
                    IdApprovalWorkFlow = awa.IdApprovalWorkFlow
                });
            }

            return timeline;
        }

        private async Task<Dictionary<int, SalaryConfigEmployeeBrief>> BuildSalaryConfigEmployeeLookupAsync(IEnumerable<int> employeeIds)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, SalaryConfigEmployeeBrief>();
            }

            return await (
                from e in _dbContext.Employees.AsNoTracking()
                join d in _dbContext.Designations.AsNoTracking() on e.IdDesignation equals d.IdDesignation into desigJoin
                from d in desigJoin.DefaultIfEmpty()
                where e.IdEmployee != null && ids.Contains(e.IdEmployee.Value)
                select new
                {
                    Id = e.IdEmployee!.Value,
                    Name = (e.FirstName + " " + (e.MiddleName ?? "") + " " + e.LastName).Trim(),
                    DesignationName = d != null ? d.DesignationName : null
                }
            ).ToDictionaryAsync(x => x.Id, x => new SalaryConfigEmployeeBrief(x.Name, x.DesignationName));
        }

        private sealed record SalaryConfigEmployeeBrief(string Name, string? DesignationName);




        private static readonly string[] ApprovedStatuses = { "APPROVED", "FINAL APPROVED" };
        private static readonly string[] PendingStatuses = { "SUBMITTED", "INTERIM APPROVED" };

        /// <summary>
        /// Structure rows with the head information (method, base head, formula) taken from SalaryHeads,
        /// sorted by Type and then Calculation Sequence.
        /// </summary>
        private async Task<List<EmployeeSalaryConfigDetailsDto>> GetConfigDetails(int idEmployeeSalaryConfig)
        {
            var rows = await (from d in _dbContext.EmployeeSalaryConfigDetails
                              join sh in _dbContext.SalaryHeads
                              on d.IdSalaryHead equals sh.IdSalaryHead into shGroup
                              from sh in shGroup.DefaultIfEmpty()
                              join ph in _dbContext.SalaryHeads
                              on sh.IdPercentageSalaryHead equals ph.IdSalaryHead into phGroup
                              from ph in phGroup.DefaultIfEmpty()
                              where d.IdEmployeeSalaryConfig == idEmployeeSalaryConfig
                              select new { d, sh, BaseHeadName = ph != null ? ph.SalaryHeadName : null })
                              .AsNoTracking()
                              .ToListAsync();

            return rows
                .Select(x => new EmployeeSalaryConfigDetailsDto
                {
                    IdEmployeeSalaryConfigDetail = x.d.IdEmployeeSalaryConfigDetail,
                    IdEmployeeSalaryConfig = x.d.IdEmployeeSalaryConfig,
                    IdSalaryHead = x.d.IdSalaryHead,
                    FixedAmount = x.d.FixedAmount,
                    PercentageValue = x.d.PercentageValue,
                    SalaryAmount = x.d.SalaryAmount,
                    SalaryHeadName = x.sh?.SalaryHeadName,
                    SalaryHeadCode = x.sh?.SalaryHeadCode,
                    HeadType = x.sh?.HeadType,
                    CalcSequence = x.sh?.CalcSequence,
                    CalculationMethod = x.sh?.CalculationMethod,
                    PercentageOfIdSalaryHead = x.sh?.IdPercentageSalaryHead,
                    PercentageOfIdSalaryHeadValue = x.BaseHeadName ?? string.Empty,
                    CustomFormula = x.sh?.CustomFormula,
                    PaidInNote = x.sh != null && !SalaryStructureCalculator.IsMonthlyHead(x.sh) ? SalaryStructureCalculator.BuildPaidInNote(x.sh) : null
                })
                .OrderBy(d => SalaryStructureCalculator.HeadTypeDisplayOrder(d.HeadType))
                .ThenBy(d => d.CalcSequence ?? int.MaxValue)
                .ToList();
        }

        public async Task<SalaryStructureResultDto> CalculateSalaryStructure(IEnumerable<SalaryStructureRowDto> rows)
        {
            var heads = await _dbContext.SalaryHeads.AsNoTracking().ToDictionaryAsync(h => h.IdSalaryHead);
            return SalaryStructureCalculator.Calculate(rows, heads);
        }

        /// <summary>The employee's current approved, active structure (latest Valid From).</summary>
        private Task<EmployeeSalaryConfig?> GetCurrentApprovedStructure(int idEmployee, int? excludeIdEmployeeSalaryConfig = null)
        {
            return _dbContext.EmployeeSalaryConfig.AsNoTracking()
                .Where(c => c.IdEmployee == idEmployee
                    && c.ApprovalStatus != null && ApprovedStatuses.Contains(c.ApprovalStatus)
                    && c.ActiveStatus != false
                    && c.IdEmployeeSalaryConfig != (excludeIdEmployeeSalaryConfig ?? 0))
                .OrderByDescending(c => c.ValidFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<EmployeeSalaryStructureInfoDto> GetEmployeeSalaryStructureInfo(int idEmployee)
        {
            var current = await GetCurrentApprovedStructure(idEmployee);
            var hasPending = await _dbContext.EmployeeSalaryConfig.AsNoTracking()
                .AnyAsync(c => c.IdEmployee == idEmployee && c.ApprovalStatus != null && PendingStatuses.Contains(c.ApprovalStatus));

            // Default Valid From: 1st of next month, and after the current structure's start
            var today = DateTime.Today;
            var defaultValidFrom = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            if (current != null && defaultValidFrom <= current.ValidFrom)
            {
                defaultValidFrom = new DateTime(current.ValidFrom.Year, current.ValidFrom.Month, 1).AddMonths(1);
            }

            var info = new EmployeeSalaryStructureInfoDto
            {
                IdEmployee = idEmployee,
                IdCurrentEmployeeSalaryConfig = current?.IdEmployeeSalaryConfig,
                CurrentValidFrom = current?.ValidFrom,
                CurrentNetSalary = current?.NetSalary,
                HasPendingStructure = hasPending,
                DefaultValidFrom = defaultValidFrom,
                DefaultRevisionReason = current == null ? "JOINING" : "INCREMENT"
            };
            if (hasPending)
            {
                info.Warnings.Add("This employee already has a salary structure waiting for approval.");
            }
            return info;
        }

        /// <summary>
        /// Checks the header and rows and calculates the amounts. Throws InvalidOperationException with the user message.
        /// Returns the calculation and any non-blocking warnings.
        /// </summary>
        private async Task<(SalaryStructureResultDto Result, List<string> Warnings)> ValidateAndCalculate(EmployeeSalaryConfigDto dto, int? idEmployeeSalaryConfig)
        {
            if (dto.IdEmployee <= 0)
                throw new InvalidOperationException("Select an employee.");
            if (!dto.ValidFrom.HasValue || dto.ValidFrom.Value.Day != 1)
                throw new InvalidOperationException("Valid From must be the first day of a month.");

            var validFrom = dto.ValidFrom.Value.Date;

            var current = await GetCurrentApprovedStructure(dto.IdEmployee, idEmployeeSalaryConfig);
            if (current != null && validFrom <= current.ValidFrom.Date)
            {
                throw new InvalidOperationException(
                    $"Valid From must be after {current.ValidFrom:dd-MM-yyyy}, the start of the current salary structure.");
            }

            var pendingExists = await _dbContext.EmployeeSalaryConfig.AsNoTracking()
                .AnyAsync(c => c.IdEmployee == dto.IdEmployee
                    && c.IdEmployeeSalaryConfig != (idEmployeeSalaryConfig ?? 0)
                    && c.ApprovalStatus != null && PendingStatuses.Contains(c.ApprovalStatus));
            if (pendingExists)
                throw new InvalidOperationException("This employee already has a salary structure waiting for approval.");

            var sameDateExists = await _dbContext.EmployeeSalaryConfig.AsNoTracking()
                .AnyAsync(c => c.IdEmployee == dto.IdEmployee
                    && c.IdEmployeeSalaryConfig != (idEmployeeSalaryConfig ?? 0)
                    && c.ValidFrom == validFrom
                    && c.ApprovalStatus != "REJECTED");
            if (sameDateExists)
                throw new InvalidOperationException("A salary configuration with the same Valid From Date already exists for this employee.");

            if (dto.IdSalaryTemplate.HasValue && dto.IdSalaryTemplate > 0)
            {
                var templateOk = await _dbContext.SalaryTemplates.AsNoTracking()
                    .AnyAsync(t => t.IdSalaryTemplate == dto.IdSalaryTemplate && t.ActiveStatus
                        && t.ApprovalStatus != null && ApprovedStatuses.Contains(t.ApprovalStatus));
                if (!templateOk)
                    throw new InvalidOperationException("Select a salary template that is approved and active.");
            }

            if (string.IsNullOrWhiteSpace(dto.RevisionReason))
            {
                if (current != null)
                    throw new InvalidOperationException("Select a revision reason.");
                dto.RevisionReason = "JOINING";
            }
            dto.RevisionReason = dto.RevisionReason.Trim().ToUpper();

            var rows = (dto.EmployeeSalaryConfigDetails ?? new List<EmployeeSalaryConfigDetailsDto>())
                .Select(d => new SalaryStructureRowDto { IdSalaryHead = d.IdSalaryHead, FixedAmount = d.FixedAmount, PercentageValue = d.PercentageValue });
            var result = await CalculateSalaryStructure(rows);
            if (result.Errors.Any())
                throw new InvalidOperationException(string.Join(" ", result.Errors));

            // Warning only: salary for the Valid From month has already been generated
            var warnings = new List<string>();
            var monthEnd = validFrom.AddMonths(1).AddDays(-1);
            var generatedMonth = await (from es in _dbContext.EmployeeSalaries
                                        join sm in _dbContext.SalaryMonths on es.IdSalaryMonth equals sm.IdSalaryMonth
                                        where es.IdEmployee == dto.IdEmployee && sm.SalaryMonthDate >= validFrom && sm.SalaryMonthDate <= monthEnd
                                        select sm.SalaryMonthDate).FirstOrDefaultAsync();
            if (generatedMonth != default)
            {
                warnings.Add($"Salary for {validFrom:MMM-yyyy} is already generated. Arrears will be needed.");
            }

            return (result, warnings);
        }

        private static void ApplyTotals(EmployeeSalaryConfig entity, SalaryStructureResultDto result)
        {
            entity.TotalEarnings = result.TotalEarnings;
            entity.TotalDeductions = result.TotalDeductions;
            entity.NetSalary = result.NetSalary;
            entity.TotalEmployerContribution = result.TotalEmployerContribution;
            entity.GrossMonthly = result.GrossMonthly;
            entity.CTCMonthly = result.CTCMonthly;
            entity.CTCAnnual = result.CTCAnnual;
        }

        public async Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee)
        {
            var (result, warnings) = await ValidateAndCalculate(dto, null);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var configEntity = new EmployeeSalaryConfig
                {
                    IdEmployee = dto.IdEmployee,
                    ValidFrom = dto.ValidFrom!.Value.Date,
                    ValidTo = null,
                    IdSalaryTemplate = dto.IdSalaryTemplate > 0 ? dto.IdSalaryTemplate : null,
                    RevisionReason = dto.RevisionReason,
                    ApprovalStatus = "SUBMITTED",
                    ActiveStatus = true,
                    CreatedBy = IdEmployee,
                    CreatedOn = DateTime.Now
                };
                ApplyTotals(configEntity, result);

                _dbContext.EmployeeSalaryConfig.Add(configEntity);
                await _dbContext.SaveChangesAsync();

                foreach (var row in result.Rows)
                {
                    _dbContext.EmployeeSalaryConfigDetails.Add(new EmployeeSalaryConfigDetails
                    {
                        IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig,
                        IdSalaryHead = row.IdSalaryHead,
                        FixedAmount = row.FixedAmount,
                        PercentageValue = row.PercentageValue,
                        SalaryAmount = row.CalculatedValue
                    });
                }
                await _dbContext.SaveChangesAsync();

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(configEntity.IdEmployeeSalaryConfig, entityCode, IdEmployee, "SUBMITTED", null, null);

                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "EmployeeSalaryConfig",
                    entityId: configEntity.IdEmployeeSalaryConfig,
                    actionDetails: new { after = BuildSalaryConfigAuditSnapshot(configEntity, result.Rows.Count), createdBy = IdEmployee });

                var saved = _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
                saved.Warnings = warnings;
                return saved;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding Employee Salary Configuration and Details.");
                throw new Exception("An error occurred while adding the configuration. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto, int IdEmployee)
        {
            var configEntity = await _dbContext.EmployeeSalaryConfig.FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == dto.IdEmployeeSalaryConfig);
            if (configEntity == null)
            {
                _logger.LogWarning("Attempt to update a non-existent Employee Salary Configuration with ID: {Id}.", dto.IdEmployeeSalaryConfig);
                return null;
            }

            // A submitted (or rejected) structure can be edited until it's approved; an approved one never is
            if (configEntity.ApprovalStatus != null && ApprovedStatuses.Contains(configEntity.ApprovalStatus))
            {
                throw new InvalidOperationException("An approved salary structure cannot be edited. Create a new structure with a new Valid From.");
            }

            // The employee of a structure cannot be changed
            dto.IdEmployee = configEntity.IdEmployee;
            var (result, warnings) = await ValidateAndCalculate(dto, configEntity.IdEmployeeSalaryConfig);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var detailCount = await _dbContext.EmployeeSalaryConfigDetails.CountAsync(d => d.IdEmployeeSalaryConfig == configEntity.IdEmployeeSalaryConfig);
                var beforeUpdate = BuildSalaryConfigAuditSnapshot(configEntity, detailCount);

                configEntity.ValidFrom = dto.ValidFrom!.Value.Date;
                configEntity.ValidTo = null;
                configEntity.IdSalaryTemplate = dto.IdSalaryTemplate > 0 ? dto.IdSalaryTemplate : null;
                configEntity.RevisionReason = dto.RevisionReason;
                configEntity.ApprovalStatus = "SUBMITTED";
                configEntity.ActiveStatus = true;
                configEntity.ModifiedBy = IdEmployee;
                configEntity.ModifiedOn = DateTime.Now;
                ApplyTotals(configEntity, result);

                // One row per head: update rows of heads still in the grid, add new heads, remove the rest
                var existingDetails = await _dbContext.EmployeeSalaryConfigDetails
                    .Where(d => d.IdEmployeeSalaryConfig == configEntity.IdEmployeeSalaryConfig)
                    .ToListAsync();

                _dbContext.EmployeeSalaryConfigDetails.RemoveRange(
                    existingDetails.Where(ed => !result.Rows.Any(r => r.IdSalaryHead == ed.IdSalaryHead)));

                foreach (var row in result.Rows)
                {
                    var existingDetail = existingDetails.FirstOrDefault(ed => ed.IdSalaryHead == row.IdSalaryHead);
                    if (existingDetail != null)
                    {
                        existingDetail.FixedAmount = row.FixedAmount;
                        existingDetail.PercentageValue = row.PercentageValue;
                        existingDetail.SalaryAmount = row.CalculatedValue;
                    }
                    else
                    {
                        _dbContext.EmployeeSalaryConfigDetails.Add(new EmployeeSalaryConfigDetails
                        {
                            IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig,
                            IdSalaryHead = row.IdSalaryHead,
                            FixedAmount = row.FixedAmount,
                            PercentageValue = row.PercentageValue,
                            SalaryAmount = row.CalculatedValue
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                // Step: Call the approval workflow service
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(configEntity.IdEmployeeSalaryConfig, entityCode, IdEmployee, "SUBMITTED", null, null);

                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "EmployeeSalaryConfig",
                    entityId: configEntity.IdEmployeeSalaryConfig,
                    actionDetails: new { before = beforeUpdate, after = BuildSalaryConfigAuditSnapshot(configEntity, result.Rows.Count), updatedBy = IdEmployee });

                var saved = _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
                saved.Warnings = warnings;
                return saved;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating Employee Salary Configuration and Details.");
                throw new Exception("An error occurred while updating the configuration. Please try again later.");
            }
        }

        public async Task<EmployeeSalaryConfigDto?> UnApproveEmployeeSalaryConfig(int idEmployeeSalaryConfig, int idEmployee)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var configEntity = await _dbContext.EmployeeSalaryConfig
                    .FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == idEmployeeSalaryConfig);
                if (configEntity == null)
                {
                    _logger.LogWarning("Unapprove: Employee Salary Configuration {Id} not found.", idEmployeeSalaryConfig);
                    return null;
                }
                if (configEntity.ValidFrom.Month != DateTime.Now.Month || configEntity.ValidFrom.Year != DateTime.Now.Year)
                {
                    throw new InvalidOperationException(
                            "Unapproval is allowed only for salary configurations within the current month.");
                }

                var lastDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                        .AddMonths(1)
                        .AddDays(-1);
                var salaryMonth = await _dbContext.SalaryMonths
                  .FirstOrDefaultAsync(s=>s.SalaryMonthDate == lastDayOfMonth.Date);

                if (salaryMonth != null)
                {
                    var alreadySalaryDone = await _dbContext.EmployeeSalaries
                       .FirstOrDefaultAsync(c => c.IdSalaryMonth == salaryMonth.IdSalaryMonth && c.IdEmployee == configEntity.IdEmployee);
                    if(alreadySalaryDone != null)
                    {
                        throw new InvalidOperationException(
                                                    "Unapproval is not allowed because Salary already generated for the current month");
                    }
                }

                var detailCount = await _dbContext.EmployeeSalaryConfigDetails
                    .CountAsync(d => d.IdEmployeeSalaryConfig == idEmployeeSalaryConfig);

                var before = BuildSalaryConfigAuditSnapshot(configEntity, detailCount);

                configEntity.ApprovalStatus = "SUBMITTED";
                configEntity.ApprovedBy = null;
                configEntity.ApprovedOn = null;
                configEntity.ModifiedBy = idEmployee;
                configEntity.ModifiedOn = DateTime.Now;
                _dbContext.EmployeeSalaryConfig.Update(configEntity);

                // The previous approved structure was closed on approval (ValidTo = day before this Valid From); reopen it
                var closedOn = configEntity.ValidFrom.Date.AddDays(-1);
                var previousStructures = await _dbContext.EmployeeSalaryConfig
                    .Where(c => c.IdEmployee == configEntity.IdEmployee
                        && c.IdEmployeeSalaryConfig != configEntity.IdEmployeeSalaryConfig
                        && c.ValidTo == closedOn)
                    .ToListAsync();
                foreach (var previous in previousStructures)
                {
                    previous.ValidTo = null;
                }
                await _dbContext.SaveChangesAsync();

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                if (string.IsNullOrWhiteSpace(entityCode))
                {
                    throw new InvalidOperationException("WorkflowEntityCodes:EmployeeSalaryConfig is not configured.");
                }

                var existingWorkflowRows = await _dbContext.ApprovalWorkFlowAllocations
                    .Where(a => a.EntityCode == entityCode && a.EntityTablePrimaryKeyID == idEmployeeSalaryConfig)
                    .ToListAsync();
                if (existingWorkflowRows.Count > 0)
                {
                    _dbContext.ApprovalWorkFlowAllocations.RemoveRange(existingWorkflowRows);
                    await _dbContext.SaveChangesAsync();
                }

                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    idEmployeeSalaryConfig,
                    entityCode,
                    idEmployee,
                    "SUBMITTED",
                    null,
                    null);

                await _dbContext.Entry(configEntity).ReloadAsync();
                var after = BuildSalaryConfigAuditSnapshot(configEntity, detailCount);

                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "EmployeeSalaryConfig",
                    entityId: idEmployeeSalaryConfig,
                    actionDetails: new
                    {
                        before,
                        after,
                        remark = "Employee salary configuration unapproved / reset to SUBMITTED; prior workflow allocations removed and workflow re-initiated.",
                        approvalWorkflowResult = approvalResult
                    });

                return await GetConfigById(idEmployeeSalaryConfig);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error unapproving Employee Salary Configuration {Id}.", idEmployeeSalaryConfig);
                throw new Exception(ex.Message);
            }
        }

        private static SalaryConfigAuditSnapshot BuildSalaryConfigAuditSnapshot(EmployeeSalaryConfig entity, int detailLineCount)
        {
            return new SalaryConfigAuditSnapshot(
                entity.IdEmployeeSalaryConfig,
                entity.IdEmployee,
                entity.ValidFrom,
                entity.ValidTo,
                entity.IdSalaryTemplate,
                entity.CreatedBy,
                entity.CreatedOn,
                entity.ApprovalStatus,
                entity.ActiveStatus,
                entity.TotalEarnings,
                entity.TotalDeductions,
                entity.NetSalary,
                detailLineCount);
        }

        private sealed record SalaryConfigAuditSnapshot(
            int IdEmployeeSalaryConfig,
            int IdEmployee,
            DateTime ValidFrom,
            DateTime? ValidTo,
            int? IdSalaryTemplate,
            int? CreatedBy,
            DateTime? CreatedOn,
            string? ApprovalStatus,
            bool? ActiveStatus,
            decimal? TotalEarnings,
            decimal? TotalDeductions,
            decimal? NetSalary,
            int DetailLineCount);

        public async Task<int?> GetNotConfiguredEmployeeCount()
        {
            var query = new StringBuilder(@"
        SELECT COUNT(DISTINCT e.IdEmployee)
        FROM Employees e
        LEFT JOIN EmployeeSalaryConfig esc ON esc.IdEmployee = e.IdEmployee
        WHERE esc.IdEmployee IS NULL");

            var parameters = new DynamicParameters();

            using (var connection = _dbContext.Database.GetDbConnection())
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                return await connection.ExecuteScalarAsync<int>(query.ToString(), parameters);
            }
        }


        #endregion


    }
}

