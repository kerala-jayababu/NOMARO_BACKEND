using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
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
                // Fetch EmployeeSalaryConfig with details in a single query
                var config = await _dbContext.EmployeeSalaryConfig
                    .Where(c => c.IdEmployeeSalaryConfig == id)
                    .Select(c => new EmployeeSalaryConfigDto
                    {
                        IdEmployeeSalaryConfig = c.IdEmployeeSalaryConfig,
                        CreatedBy = (int)c.CreatedBy,
                        CreatedByValue = _dbContext.Employees
                            .Where(e => e.IdEmployee == c.CreatedBy)
                            .Select(e => e.FirstName + " " + e.MiddleName + " " + e.LastName)
                            .FirstOrDefault(),
                        IdEmployee = c.IdEmployee,
                        ValidFrom = c.ValidFrom,
                        ValidTo = c.ValidTo,
                        IdSalaryTemplate = c.IdSalaryTemplate,
                        ApprovalStatus = c.ApprovalStatus,
                        ActiveStatus = c.ActiveStatus,
                        TotalEarnings = c.TotalEarnings,
                        CreatedOn = (DateTime)c.CreatedOn,
                        TotalDeductions = c.TotalDeductions,
                        NetSalary = c.NetSalary,

                        EmployeeSalaryConfigDetails = _dbContext.EmployeeSalaryConfigDetails
                            .Where(d => d.IdEmployeeSalaryConfig == c.IdEmployeeSalaryConfig)
                            .Select(d => new EmployeeSalaryConfigDetailsDto
                            {
                                IdEmployeeSalaryConfig = d.IdEmployeeSalaryConfig,
                                CustomFormula = d.CustomFormula,
                                SalaryAmount = d.SalaryAmount,
                                PercentageOfIdSalaryHead = d.PercentageOfIdSalaryHead,
                                IdEmployeeSalaryConfigDetail = d.IdEmployeeSalaryConfigDetail,
                                IdSalaryHead = d.IdSalaryHead,
                                FixedAmount = d.FixedAmount,
                                PercentageValue = d.PercentageValue,
                                CalculationMethod = d.CalculationMethod,
                                SalaryHeadName = _dbContext.SalaryHeads
                                    .Where(sh => sh.IdSalaryHead == d.IdSalaryHead)
                                    .Select(sh => sh.SalaryHeadName)
                                    .FirstOrDefault(),
                                HeadType = _dbContext.SalaryHeads
                                    .Where(sh => sh.IdSalaryHead == d.IdSalaryHead)
                                    .Select(sh => sh.HeadType)
                                    .FirstOrDefault(),
                                SalaryHeadCode = _dbContext.SalaryHeads
                                    .Where(sh => sh.IdSalaryHead == d.IdSalaryHead)
                                    .Select(sh => sh.SalaryHeadCode)
                                    .FirstOrDefault(),
                                PercentageOfIdSalaryHeadValue = _dbContext.SalaryHeads
                    .Where(ph => ph.IdSalaryHead == d.PercentageOfIdSalaryHead)
                    .Select(ph => ph.SalaryHeadName)
                    .FirstOrDefault() ?? string.Empty
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

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

                // Do not dispose this connection — it is owned by the DbContext. Disposing it breaks later EF queries
                // on the same context (e.g. GetLatestApprovedConfigByEmployeeId → BuildSalaryConfigApprovalStepsAsync).
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




        public async Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (dto.ValidFrom.HasValue && dto.IdEmployee > 0)
                {
                    var exists = await EmployeeSalaryConfigExistsForEmployeeOnValidFromDateAsync(dto.IdEmployee, dto.ValidFrom.Value);
                    if (exists)
                    {
                        throw new InvalidOperationException(
                            "A salary configuration with this Valid From date already exists for this employee.");
                    }
                }

                // Map and insert EmployeeSalaryConfig
                var configEntity = _mapper.Map<EmployeeSalaryConfig>(dto);
                configEntity.ApprovalStatus = "SUBMITTED";
                configEntity.CreatedBy = IdEmployee;
                configEntity.CreatedOn = DateTime.Now;

                _dbContext.EmployeeSalaryConfig.Add(configEntity);
                await _dbContext.SaveChangesAsync();

                // Insert related EmployeeSalaryConfigDetails
                if (dto.EmployeeSalaryConfigDetails != null && dto.EmployeeSalaryConfigDetails.Any())
                {
                    foreach (var detailDto in dto.EmployeeSalaryConfigDetails)
                    {
                        var detailEntity = new EmployeeSalaryConfigDetails
                        {
                            IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig,
                            IdSalaryHead = detailDto.IdSalaryHead,
                            CalculationMethod = detailDto.CalculationMethod,
                            FixedAmount = detailDto.FixedAmount,
                            PercentageOfIdSalaryHead = detailDto.PercentageOfIdSalaryHead,
                            PercentageValue = detailDto.PercentageValue,
                            CustomFormula = detailDto.CustomFormula,
                            SalaryAmount = detailDto.SalaryAmount,
                        };

                        _dbContext.EmployeeSalaryConfigDetails.Add(detailEntity);
                    }
                    await _dbContext.SaveChangesAsync();


                }

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(configEntity.IdEmployeeSalaryConfig, entityCode, IdEmployee, "SUBMITTED", null, null);

                //if (approvalResult != "Approval workflow initiated.")
                //{
                //    throw new Exception(approvalResult);
                //}

                await transaction.CommitAsync();
                return _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding Employee Salary Configuration and Details.");
                throw new Exception("An error occurred while adding the configuration. Please try again later.");
            }
        }

        private Task<bool> EmployeeSalaryConfigExistsForEmployeeOnValidFromDateAsync(int idEmployee, DateTime validFrom)
        {
            var dayStart = validFrom.Date;
            var dayEnd = dayStart.AddDays(1);
            return _dbContext.EmployeeSalaryConfig.AsNoTracking()
                .AnyAsync(c => c.IdEmployee == idEmployee && c.ValidFrom >= dayStart);
        }


        public async Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto, int IdEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Update EmployeeSalaryConfig
                var configEntity = await _dbContext.EmployeeSalaryConfig.FirstOrDefaultAsync(c => c.IdEmployeeSalaryConfig == dto.IdEmployeeSalaryConfig);
                if (configEntity == null)
                {
                    _logger.LogWarning("Attempt to update a non-existent Employee Salary Configuration with ID: {Id}.", dto.IdEmployeeSalaryConfig);
                    return null;
                }

                // Manual mapping for update
                configEntity.IdEmployee = dto.IdEmployee;
                configEntity.ValidFrom = (DateTime)dto.ValidFrom;
                configEntity.ValidTo = dto.ValidTo;
                configEntity.IdSalaryTemplate = dto.IdSalaryTemplate;
                configEntity.TotalEarnings = dto.TotalEarnings;
                configEntity.TotalDeductions = dto.TotalDeductions;
                configEntity.NetSalary = dto.NetSalary;
                configEntity.ApprovalStatus = dto.ApprovalStatus;
                configEntity.ActiveStatus = dto.ActiveStatus;

                _dbContext.EmployeeSalaryConfig.Update(configEntity);
                await _dbContext.SaveChangesAsync();

                // Update EmployeeSalaryConfigDetails
                if (dto.EmployeeSalaryConfigDetails != null)
                {
                    var existingDetails = await _dbContext.EmployeeSalaryConfigDetails
                        .Where(d => d.IdEmployeeSalaryConfig == configEntity.IdEmployeeSalaryConfig)
                        .ToListAsync();

                    // Delete details that are no longer in the DTO
                    var detailsToDelete = existingDetails
                        .Where(d => !dto.EmployeeSalaryConfigDetails.Any(dtoDetail => dtoDetail.IdEmployeeSalaryConfigDetail == d.IdEmployeeSalaryConfigDetail))
                        .ToList();
                    _dbContext.EmployeeSalaryConfigDetails.RemoveRange(detailsToDelete);

                    // Update existing details
                    foreach (var detailDto in dto.EmployeeSalaryConfigDetails)
                    {
                        var existingDetail = existingDetails.FirstOrDefault(d => d.IdEmployeeSalaryConfigDetail == detailDto.IdEmployeeSalaryConfigDetail);
                        if (existingDetail != null)
                        {
                            existingDetail.IdSalaryHead = detailDto.IdSalaryHead;
                            existingDetail.CalculationMethod = detailDto.CalculationMethod;
                            existingDetail.FixedAmount = detailDto.FixedAmount;
                            existingDetail.PercentageOfIdSalaryHead = detailDto.PercentageOfIdSalaryHead;
                            existingDetail.PercentageValue = detailDto.PercentageValue;
                            existingDetail.CustomFormula = detailDto.CustomFormula;
                            existingDetail.SalaryAmount = detailDto.SalaryAmount;
                            _dbContext.EmployeeSalaryConfigDetails.Update(existingDetail);
                        }
                        else
                        {
                            // Add new details
                            var trackedEntity = _dbContext.ChangeTracker.Entries<EmployeeSalaryConfigDetails>()
            .FirstOrDefault(e => e.Entity.IdEmployeeSalaryConfigDetail == detailDto.IdEmployeeSalaryConfigDetail);

                            if (trackedEntity != null)
                            {
                                _dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
                            }

                            // Add new details
                            var newDetailEntity = new EmployeeSalaryConfigDetails
                            {
                                IdEmployeeSalaryConfigDetail = null,
                                IdEmployeeSalaryConfig = configEntity.IdEmployeeSalaryConfig,
                                IdSalaryHead = detailDto.IdSalaryHead,
                                CalculationMethod = detailDto.CalculationMethod,
                                FixedAmount = detailDto.FixedAmount,
                                PercentageValue = detailDto.PercentageValue,
                                PercentageOfIdSalaryHead = detailDto.PercentageOfIdSalaryHead,
                                CustomFormula = detailDto.CustomFormula,
                                SalaryAmount = detailDto.SalaryAmount
                            };
                            _dbContext.EmployeeSalaryConfigDetails.Add(newDetailEntity);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }
                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                // Step: Call the approval workflow service
                var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow((int)dto.IdEmployeeSalaryConfig, entityCode, IdEmployee, "SUBMITTED", null, null);

                await transaction.CommitAsync();
                return _mapper.Map<EmployeeSalaryConfigDto>(configEntity);
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
                    _logger.LogWarning("Submit for approval: Employee Salary Configuration {Id} not found.", idEmployeeSalaryConfig);
                    return null;
                }

                var detailCount = await _dbContext.EmployeeSalaryConfigDetails
                    .CountAsync(d => d.IdEmployeeSalaryConfig == idEmployeeSalaryConfig);

                var before = BuildSalaryConfigAuditSnapshot(configEntity, detailCount);

                configEntity.ApprovalStatus = "SUBMITTED";
                _dbContext.EmployeeSalaryConfig.Update(configEntity);
                await _dbContext.SaveChangesAsync();

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"];
                if (string.IsNullOrWhiteSpace(entityCode))
                {
                    throw new InvalidOperationException("WorkflowEntityCodes:EmployeeSalaryConfig is not configured.");
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
                        remark = "Employee salary configuration submitted for approval (approval status set to SUBMITTED).",
                        approvalWorkflowResult = approvalResult
                    });

                return await GetConfigById(idEmployeeSalaryConfig);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting Employee Salary Configuration {Id} for approval.", idEmployeeSalaryConfig);
                throw new Exception("An error occurred while submitting the configuration for approval. Please try again later.");
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
