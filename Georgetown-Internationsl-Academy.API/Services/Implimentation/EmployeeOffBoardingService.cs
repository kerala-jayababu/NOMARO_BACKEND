using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using static Georgetown_Internationsl_Academy.API.Services.Implimentation.EmployeeOffBoardingService;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeOffBoardingService : IEmployeeOffBoarding
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<AssetServices> _logger;
        private readonly IConfiguration _configuration;

        public EmployeeOffBoardingService(ApplicationDBContext dbContext, IMapper mapper, ILogger<AssetServices> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        #region OFFBOARDING CONFIGURATIONS

        public async Task<IEnumerable<ExitReasonDto>> GetExitReasons()
        {
            var data = await _dbContext.ExitReasons
                .OrderBy(x => x.ReasonName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ExitReasonDto>>(data);
        }

        public async Task<bool> AddOrUpdateExitReasons(ExitReasonDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ExitReasons.ToListAsync();
                var entity = existing.FirstOrDefault(x => x.IdExitReason == dto.IdExitReason);
                if (entity != null)
                {
                    entity.ReasonCode = dto.ReasonCode;
                    entity.ReasonName = dto.ReasonName;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedAt = DateTime.Now;
                }
                else
                {
                    await _dbContext.ExitReasons.AddAsync(new ExitReasons
                    {
                        ReasonCode = dto.ReasonCode,
                        ReasonName = dto.ReasonName,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now
                    });
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

        public async Task<IEnumerable<ExitTypeDto>> GetExitTypes()
        {
            var data = await _dbContext.ExitTypes
                .OrderBy(x => x.TypeName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ExitTypeDto>>(data);
        }

        public async Task<bool> AddOrUpdateExitTypes(ExitTypeDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ExitTypes.ToListAsync();

                var entity = existing.FirstOrDefault(x => x.IdExitType == dto.IdExitType);
                if (entity != null)
                {
                    entity.TypeCode = dto.TypeCode;
                    entity.TypeName = dto.TypeName;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedAt = DateTime.Now;
                }
                else
                {
                    await _dbContext.ExitTypes.AddAsync(new ExitTypes
                    {
                        TypeCode = dto.TypeCode,
                        TypeName = dto.TypeName,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now
                    });
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
        public async Task<IEnumerable<NoticePeriodPolicyDto>> GetNoticePeriodPolicies()
        {
            var data = await _dbContext.NoticePeriodPolicies
                .OrderBy(x => x.PolicyName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<NoticePeriodPolicyDto>>(data);
        }

        public async Task<bool> AddOrUpdateNoticePeriodPolicies(NoticePeriodPolicyDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.NoticePeriodPolicies.ToListAsync();

                var entity = existing.FirstOrDefault(x => x.IdNoticePeriodPolicy == dto.IdNoticePeriodPolicy);
                if (entity != null)
                {
                    entity.PolicyCode = dto.PolicyCode;
                    entity.PolicyName = dto.PolicyName;
                    entity.AppliesToEmployeeType = dto.AppliesToEmployeeType;
                    entity.NoticeDays = dto.NoticeDays;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedAt = DateTime.Now;
                }
                else
                {
                    await _dbContext.NoticePeriodPolicies.AddAsync(new NoticePeriodPolicies
                    {
                        PolicyCode = dto.PolicyCode,
                        PolicyName = dto.PolicyName,
                        AppliesToEmployeeType = dto.AppliesToEmployeeType,
                        NoticeDays = dto.NoticeDays,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now
                    });
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

        public async Task<IEnumerable<ClearanceTemplateDto>> GetClearanceTemplates()
        {
            var data = await _dbContext.ClearanceTemplates
                .OrderBy(x => x.TemplateName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ClearanceTemplateDto>>(data);
        }

        public async Task<bool> AddOrUpdateClearanceTemplates(ClearanceTemplateDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ClearanceTemplates.ToListAsync();
                var entity = existing.FirstOrDefault(x => x.IdClearanceTemplate == dto.IdClearanceTemplate);
                if (entity != null)
                {
                    entity.TemplateName = dto.TemplateName;
                    entity.Description = dto.Description;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedAt = DateTime.Now;
                }
                else
                {
                    await _dbContext.ClearanceTemplates.AddAsync(new ClearanceTemplates
                    {
                        TemplateName = dto.TemplateName,
                        Description = dto.Description,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now
                    });
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

        public async Task<IEnumerable<ClearanceTemplateDepartmentDto>> GetClearanceTemplateDepartments(int idClearanceTemplate)
        {
            return await _dbContext.ClearanceTemplateDepartments
                .Where(x => x.IdClearanceTemplate == idClearanceTemplate)
                .Select(x => new ClearanceTemplateDepartmentDto
                {
                    IdTemplateDept = x.IdTemplateDept,
                    IdClearanceTemplate = x.IdClearanceTemplate,
                    IdDepartment = x.IdDepartment,
                    CheckListItem = x.CheckListItem,
                    IsMandatory = x.IsMandatory
                })
                .ToListAsync();
        }

        public async Task<bool> AddOrUpdateClearanceTemplateDepartment(List<ClearanceTemplateDepartmentDto> dtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var templateId = dtos.First().IdClearanceTemplate;

                var existing = await _dbContext.ClearanceTemplateDepartments
                    .Where(x => x.IdClearanceTemplate == templateId)
                    .ToListAsync();

                // Delete removed items
                _dbContext.ClearanceTemplateDepartments.RemoveRange(existing
                    .Where(e => !dtos.Any(d => d.IdTemplateDept == e.IdTemplateDept)));

                foreach (var dto in dtos)
                {
                    var entity = existing.FirstOrDefault(x => x.IdTemplateDept == dto.IdTemplateDept);
                    if (entity != null)
                    {
                        entity.IdDepartment = dto.IdDepartment;
                        entity.CheckListItem = dto.CheckListItem;
                        entity.IsMandatory = dto.IsMandatory;
                    }
                    else
                    {
                        await _dbContext.ClearanceTemplateDepartments.AddAsync(
                            new ClearanceTemplateDepartments
                            {
                                IdClearanceTemplate = dto.IdClearanceTemplate,
                                IdDepartment = dto.IdDepartment,
                                CheckListItem = dto.CheckListItem,
                                IsMandatory = dto.IsMandatory
                            });
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving ClearanceTemplateDepartments");
                throw;
            }
        }

        #endregion
        #region EXIT CASES

        public async Task<SubmitResignationResponseDto> SubmitResignation(SubmitResignationDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var errors = new List<string>();

                // Check if updating existing or creating new
                ExitCases? existingExitCase = null;
                if (dto.IdExitCase > 0)
                {
                    existingExitCase = await _dbContext.ExitCases.FirstOrDefaultAsync(e => e.IdExitCase == dto.IdExitCase);
                    if (existingExitCase == null)
                    {
                        return new SubmitResignationResponseDto
                        {
                            Success = false,
                            Message = "Exit case not found.",
                            Errors = new List<string> { $"Exit case with ID {dto.IdExitCase} does not exist." }
                        };
                    }
                }

                // Validate employee exists
                var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.IdEmployee == dto.IdEmployee);
                if (employee == null)
                {
                    return new SubmitResignationResponseDto
                    {
                        Success = false,
                        Message = "Employee not found.",
                        Errors = new List<string> { $"Employee with ID {dto.IdEmployee} does not exist." }
                    };
                }

                // Validate exit reason exists
                var exitReason = await _dbContext.ExitReasons.FirstOrDefaultAsync(x => x.IdExitReason == dto.IdExitReason && x.IsActive);
                if (exitReason == null)
                {
                    return new SubmitResignationResponseDto
                    {
                        Success = false,
                        Message = "Invalid exit reason.",
                        Errors = new List<string> { $"Exit reason with ID {dto.IdExitReason} does not exist or is inactive." }
                    };
                }
       
                string employeeWorkType = "FULLTIME";

                // Get Notice Period Policy based on employee work type
                var noticePolicy = await _dbContext.NoticePeriodPolicies
                    .FirstOrDefaultAsync(x =>
                        x.AppliesToEmployeeType == employeeWorkType &&
                        x.IsActive);

                if (noticePolicy == null)
                {
                    return new SubmitResignationResponseDto
                    {
                        Success = false,
                        Message = $"Notice period policy not found for employee type '{employeeWorkType}'.",
                        Errors = new List<string> { $"No active notice period policy exists for employee type '{employeeWorkType}'." }
                    };
                }

                // Get Reporting Officer
                int? reportingOfficerId = employee.ReportingTo;
                if (!reportingOfficerId.HasValue)
                {
                    return new SubmitResignationResponseDto
                    {
                        Success = false,
                        Message = "Reporting officer not assigned to employee.",
                        Errors = new List<string> { "Employee must have a reporting officer assigned." }
                    };
                }

                // Validate reporting officer exists
                var reportingOfficer = await _dbContext.Employees.FirstOrDefaultAsync(e => e.IdEmployee == reportingOfficerId.Value);
                if (reportingOfficer == null)
                {
                    return new SubmitResignationResponseDto
                    {
                        Success = false,
                        Message = "Reporting officer not found.",
                        Errors = new List<string> { $"Reporting officer with ID {reportingOfficerId} does not exist." }
                    };
                }

                // Calculate effective notice days and earliest LWD
                int effectiveNoticeDays = noticePolicy.NoticeDays;             

                // UPDATE existing exit case
                if (existingExitCase != null)
                {
                    existingExitCase.IdExitReason = dto.IdExitReason;
                    existingExitCase.EmployeeReasonDetails = dto.EmployeeReasonDetails;
                    existingExitCase.ProposedLWD = dto.ProposedLWD;
                    existingExitCase.IdNoticePolicy = noticePolicy.IdNoticePeriodPolicy;
                    existingExitCase.PolicyNoticeDays = noticePolicy.NoticeDays;
                    existingExitCase.EffectiveNoticeDays = effectiveNoticeDays;              
                    existingExitCase.UpdatedBy = loggedInEmployeeId;
                    existingExitCase.UpdatedAt = DateTime.Now;
                    existingExitCase.PendingWithIDEmployee = reportingOfficerId.Value;

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new SubmitResignationResponseDto
                    {
                        Success = true,
                        IdExitCase = existingExitCase.IdExitCase,
                        CaseNumber = existingExitCase.CaseNumber,
                        Message = "Resignation updated successfully. Your reporting officer will review and approve the resignation."
                    };
                }
                else
                {
                    // CREATE new exit case
                    var caseNumber = await GenerateCaseNumber();

                    var exitCase = new ExitCases
                    {
                        CaseNumber = caseNumber,
                        IdEmployee = dto.IdEmployee,
                        IdExitType = 1, // Default exit type for resignation (can be parameterized)
                        IdExitReason = dto.IdExitReason,
                        InitiationDate = DateTime.Now,
                        EmployeeReasonDetails = dto.EmployeeReasonDetails,
                        ProposedLWD = dto.ProposedLWD,
                        ApprovedLWD = null,
                        IdNoticePolicy = noticePolicy.IdNoticePeriodPolicy,
                        PolicyNoticeDays = noticePolicy.NoticeDays,
                        IsNoticeOverridden = false,
                        EffectiveNoticeDays = effectiveNoticeDays,
                       
                        HandoverPlan = null,
                        ExitInterviewDate = null,
                        ContactAfterExit = null,
                        ExitStatus = "INITIATED",
                        PendingWith = "REPOFFICER",
                        IdClearanceTemplate = null,
                        AssignedClearanceTemplateBy = null,
                        ClearanceInitiatedOn = null,
                        CreatedBy = loggedInEmployeeId,
                        CreatedAt = DateTime.Now,
                        UpdatedBy = null,
                        UpdatedAt = null,
                        PendingWithIDEmployee = reportingOfficerId.Value
                    };

                    await _dbContext.ExitCases.AddAsync(exitCase);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new SubmitResignationResponseDto
                    {
                        Success = true,
                        IdExitCase = exitCase.IdExitCase,
                        CaseNumber = exitCase.CaseNumber,
                        Message = "Resignation submitted successfully. Your reporting officer will review and approve the resignation."
                    };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting resignation");
                return new SubmitResignationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while submitting resignation.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private async Task<string> GenerateCaseNumber()
        {
            // Format: EXIT-YYYYMMDD-XXXX (e.g., EXIT-20250118-0001)
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyyyMMdd");

            var lastCaseNumber = await _dbContext.ExitCases
                .Where(x => x.CaseNumber != null && x.CaseNumber.StartsWith($"EXIT-{datePrefix}"))
                .OrderByDescending(x => x.CaseNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastCaseNumber != null && !string.IsNullOrEmpty(lastCaseNumber.CaseNumber))
            {
                var lastSequence = lastCaseNumber.CaseNumber.Split('-').Last();
                if (int.TryParse(lastSequence, out int sequence))
                {
                    nextSequence = sequence + 1;
                }
            }

            return $"EXIT-{datePrefix}-{nextSequence:D4}";
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

        public async Task<IEnumerable<ResignationRequestDto>> GetResignationRequests(int idLoggedInEmployee, string? roleType = null, int? idEmployee = null, DateTime? initiationDate = null)
        {
            try
            {
                // Validate inputs
                // If IdEmployee is provided (> 0), roleType is optional
                // If IdEmployee is not provided, roleType is mandatory
                if ((!idEmployee.HasValue || idEmployee <= 0) && string.IsNullOrEmpty(roleType))
                {
                    throw new ArgumentException("Either IdEmployee or RoleType must be provided. If IdEmployee is not provided, RoleType is mandatory. RoleType values: REPOFFICER, HREXECUTIVE, HRHEAD");
                }

                // Validate RoleType if provided
                var validRoles = new[] { "REPOFFICER", "HREXECUTIVE", "HRHEAD" };
                if (!string.IsNullOrEmpty(roleType) && !validRoles.Contains(roleType.ToUpper()))
                {
                    throw new ArgumentException("Invalid RoleType. Valid values: REPOFFICER, HREXECUTIVE, HRHEAD");
                }

                // Get logged-in employee details
                var loggedInEmployee = await _dbContext.Employees
                    .FirstOrDefaultAsync(e => e.IdEmployee == idLoggedInEmployee);

                if (loggedInEmployee == null)
                {
                    throw new Exception("Logged-in employee not found.");
                }

                // Build the query
                var query = new StringBuilder(@"
        SELECT 
            ec.IdExitCase,
            ec.CaseNumber,
            ec.InitiationDate,
            ec.ExitStatus,
            ec.PendingWith,
            
            -- Resigning Employee Info
            e.IdEmployee,
            e.EmployeeCode,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName, ''), ' ', e.LastName) AS EmployeeName,
            e.IdDepartment AS IdEmployeeDepartment,
            d.DepartmentName AS EmployeeDepartmentName,
            e.IdDesignation AS IdEmployeeDesignation,
            des.DesignationName AS EmployeeDesignationName,
            
            -- Exit Type Info
            ec.IdExitType,
            et.TypeCode AS ExitTypeCode,
            et.TypeName AS ExitTypeName,
            
            -- Exit Reason Info
            ec.IdExitReason,
            er.ReasonCode AS ExitReasonCode,
            er.ReasonName AS ExitReasonName,
            ec.EmployeeReasonDetails,
            ec.ProposedLWD,
            ec.ApprovedLWD,           
            
            -- Notice Period Info
            ec.IdNoticePolicy,
            npp.PolicyCode AS NoticePolicyCode,
            npp.PolicyName AS NoticePolicyName,
            ec.PolicyNoticeDays,
            ec.EffectiveNoticeDays,
            ec.IsNoticeOverridden,
            
            -- Clearance Info
            ec.IdClearanceTemplate,
            ct.TemplateName AS ClearanceTemplateName,
            ct.Description AS ClearanceTemplateDescription,
            ec.ClearanceInitiatedOn,
            
            -- Reporting Officer Info
            ec.PendingWithIDEmployee,
            ro.EmployeeCode AS ReportingOfficerCode,
            CONCAT(ro.FirstName, ' ', COALESCE(ro.MiddleName, ''), ' ', ro.LastName) AS ReportingOfficerName,
            ro.IdDepartment AS ReportingOfficerDepartment,
            rod.DepartmentName AS ReportingOfficerDepartmentName,
            
            -- Metadata
            ec.CreatedAt,
            ec.CreatedBy
        FROM ExitCases ec
        INNER JOIN Employees e ON ec.IdEmployee = e.IdEmployee
        INNER JOIN Departments d ON e.IdDepartment = d.IdDepartment
        INNER JOIN Designations des ON e.IdDesignation = des.IdDesignation
        LEFT JOIN ExitTypes et ON ec.IdExitType = et.IdExitType
        LEFT JOIN ExitReasons er ON ec.IdExitReason = er.IdExitReason
        LEFT JOIN NoticePeriodPolicies npp ON ec.IdNoticePolicy = npp.IdNoticePeriodPolicy
        LEFT JOIN ClearanceTemplates ct ON ec.IdClearanceTemplate = ct.IdClearanceTemplate
        LEFT JOIN Employees ro ON ec.PendingWithIDEmployee = ro.IdEmployee
        LEFT JOIN Departments rod ON ro.IdDepartment = rod.IdDepartment
        WHERE 1=1
    ");

                var parameters = new DynamicParameters();

                // Filter logic based on RoleType and IdEmployee
                if (idEmployee.HasValue && idEmployee > 0)
                {
                    // If IdEmployee provided, get all cases for that specific employee
                    query.Append(" AND e.IdEmployee = @IdEmployee ");
                    parameters.Add("IdEmployee", idEmployee.Value);
                }
                else
                {
                    // RoleType-based filtering
                    roleType = roleType?.ToUpper();

                    if (roleType == "REPOFFICER")
                    {
                        // Get all employees reporting to the logged-in employee (directly and indirectly)
                        var reportingEmployeeIds = await GetEmployeeHierarchy(idLoggedInEmployee);
                        
                        if (reportingEmployeeIds.Any())
                        {
                            // Build IN clause for employee IDs
                            var employeeIdList = string.Join(",", reportingEmployeeIds);
                            query.Append($" AND e.IdEmployee IN ({employeeIdList}) ");
                        }
                        else
                        {
                            // No employees reporting to this officer
                            query.Append(" AND 1=0 ");
                        }
                    }
                    else if (roleType == "HREXECUTIVE" || roleType == "HRHEAD")
                    {
                        // Get all resignation requests from HR department
                        // The logged-in employee must be from HR department
                        var hrDepartmentCode = _configuration["Departments:HRCode"] ?? "HRD";
                        
                        // For now, filter by employees pending with any HR role
                        // OR filter by all cases if logged-in employee is from HR
                        query.Append(@"
            AND (
                LOWER(d.DepartmentCode) = @HRDeptCode
                OR ec.PendingWith IN ('HRMANAGER', 'HROFFICER')
            )
        ");
                        parameters.Add("HRDeptCode", hrDepartmentCode.ToLower());
                    }
                }

                // Optional InitiationDate filter
                if (initiationDate.HasValue)
                {
                    query.Append(" AND CAST(ec.InitiationDate AS DATE) >= @InitiationDate ");
                    parameters.Add("InitiationDate", initiationDate.Value.Date);
                }

                query.Append(" ORDER BY ec.InitiationDate DESC ");

                try
                {
                    using (var connection = _dbContext.Database.GetDbConnection())
                    {
                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();

                        var exitCases = await connection.QueryAsync<ResignationRequestDto>(query.ToString(), parameters);
                        if (!exitCases.Any())
                            return exitCases;

                        var exitCaseIds = exitCases.Select(x => x.IdExitCase).Distinct().ToList();

                        // 2️⃣ LOAD STATUS HISTORY
                        var histories = await connection.QueryAsync<ExitCaseStatusHistoryDto>(
                            @"SELECT 
            IdExitCase,
            ActionType,
            FromStatus,
            ToStatus,
            PendingWith,
            CreatedBy,
            CreatedAt
          FROM ExitCaseStatusHistory
          WHERE IdExitCase IN @Ids",
                            new { Ids = exitCaseIds }
                        );

                        // 3️⃣ LOAD CLEARANCE ASSIGNMENTS
                        var assignments = await connection.QueryAsync<ExitCaseClearanceAssignmentDto>(
                            @"SELECT
        a.IdExitCase,
        a.IdDepartment,
        d.DepartmentName,
        a.IdAssigneeUser,
        a.DeptClearanceStatus,
        a.AssignedAt
      FROM ExitCaseClearanceAssignments a
      INNER JOIN Departments d ON d.IdDepartment = a.IdDepartment
      WHERE a.IdExitCase IN @Ids",
                            new { Ids = exitCaseIds }
                        );

                        // 4️⃣ LOAD CLEARANCE LINES
                        var clearanceLines = await connection.QueryAsync<ExitCaseDepartmentClearanceLineDto>(
                         @"SELECT
        l.IdExitCase,
        l.IdDepartment,
        d.DepartmentName,
        l.CheckListItem,
        l.DeptClearanceStatus,
        l.SortOrder
      FROM ExitCaseDepartmentClearanceLines l
      INNER JOIN Departments d ON d.IdDepartment = l.IdDepartment
      WHERE l.IdExitCase IN @Ids
      ORDER BY l.IdExitCase, l.IdDepartment, l.SortOrder",
                            new { Ids = exitCaseIds }
                        );

                        // 5️⃣ MAP CHILD DATA
                        var historyLookup = histories.GroupBy(x => x.IdExitCase)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        var assignmentLookup = assignments.GroupBy(x => x.IdExitCase)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        var lineLookup = clearanceLines.GroupBy(x => x.IdExitCase)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        foreach (var exitCase in exitCases)
                        {
                            exitCase.ExitCaseHistories =
                                historyLookup.GetValueOrDefault(exitCase.IdExitCase, new());

                            exitCase.ClearanceAssignments =
                                assignmentLookup.GetValueOrDefault(exitCase.IdExitCase, new());

                            exitCase.DepartmentClearanceLines =
                                lineLookup.GetValueOrDefault(exitCase.IdExitCase, new());
                        }

                        return exitCases;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing resignation requests query");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching resignation requests for Employee ID: {employeeId}", idLoggedInEmployee);
                throw;
            }
        }

        public async Task<ReportingOfficerActionResponseDto> SubmitReportingOfficerActions(
                  SubmitReportingOfficerActionsDto dto,
                  int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate Exit Case
                var exitCase = await _dbContext.ExitCases
                    .FirstOrDefaultAsync(e => e.IdExitCase == dto.IdExitCase);

                if (exitCase == null)
                {
                    return new ReportingOfficerActionResponseDto
                    {
                        Success = false,
                        IdExitCase = dto.IdExitCase,
                        Message = "Exit case not found."
                    };
                }

                // 2. Validate Employee
                var employee = await _dbContext.Employees
                    .FirstOrDefaultAsync(e => e.IdEmployee == dto.IdEmployee);

                if (employee == null)
                {
                    return new ReportingOfficerActionResponseDto
                    {
                        Success = false,
                        IdExitCase = dto.IdExitCase,
                        Message = "Employee not found."
                    };
                }

                // 3. Authorization check
                if (exitCase.PendingWithIDEmployee != loggedInEmployeeId)
                {
                    return new ReportingOfficerActionResponseDto
                    {
                        Success = false,
                        IdExitCase = dto.IdExitCase,
                        Message = "Unauthorized action."
                    };
                }

                // 4. Status validation
                if (exitCase.ExitStatus != "INITIATED")
                {
                    return new ReportingOfficerActionResponseDto
                    {
                        Success = false,
                        IdExitCase = dto.IdExitCase,
                        Message = "Invalid exit case status."
                    };
                }

                // 5. Validate Action
                if (dto.Action != "Approved" && dto.Action != "Rejected")
                {
                    return new ReportingOfficerActionResponseDto
                    {
                        Success = false,
                        IdExitCase = dto.IdExitCase,
                        Message = "Action must be Approved or Rejected."
                    };
                }

                // 6. Approved LWD validation
                int effectiveNoticeDays = 0;
                if (dto.Action == "Approved")
                {
                    if (!dto.ApprovedLWD.HasValue)
                    {
                        return new ReportingOfficerActionResponseDto
                        {
                            Success = false,
                            IdExitCase = dto.IdExitCase,
                            Message = "ApprovedLWD is required."
                        };
                    }

                    effectiveNoticeDays =
                        (dto.ApprovedLWD.Value.Date - exitCase.InitiationDate.Date).Days;

                    if (effectiveNoticeDays < 0)
                    {
                        return new ReportingOfficerActionResponseDto
                        {
                            Success = false,
                            IdExitCase = dto.IdExitCase,
                            Message = "ApprovedLWD cannot be before Initiation Date."
                        };
                    }
                }

                // 7. Get HR Officer
                int? hrOfficerId = null;
                if (dto.Action == "Approved")
                {
                    var hrDesignation = await _dbContext.Designations
                        .FirstOrDefaultAsync(d => d.DesignationCode == "HRD");

                    if (hrDesignation == null)
                        throw new Exception("HR designation not found.");

                    var hrOfficer = await _dbContext.Employees
                        .FirstOrDefaultAsync(e => e.IdDesignation == hrDesignation.IdDesignation);

                    if (hrOfficer == null)
                        throw new Exception("HR officer not assigned.");

                    hrOfficerId = hrOfficer.IdEmployee;
                }

                // 8. Get Order Number
                var lastOrderNumber = await _dbContext.ExitCaseStatusHistory
                    .Where(h => h.IdExitCase == dto.IdExitCase)
                    .OrderByDescending(h => h.OrderNumber)
                    .Select(h => h.OrderNumber)
                    .FirstOrDefaultAsync();

                int newOrderNumber = lastOrderNumber + 1;

                // 9. Update Exit Case
                if (dto.Action == "Approved")
                {
                    exitCase.ApprovedLWD = dto.ApprovedLWD;
                    exitCase.EffectiveNoticeDays = effectiveNoticeDays;
                    exitCase.HandoverPlan = dto.HandOverNotes;
                    exitCase.ExitStatus = "RepOfficerApproved";
                    exitCase.PendingWith = "HROFFICER";
                    exitCase.PendingWithIDEmployee = hrOfficerId;
                }
                else
                {
                    exitCase.HandoverPlan = dto.HandOverNotes;
                    exitCase.ExitStatus = "RepOfficerRejected";
                    exitCase.PendingWith = "EMPLOYEE";
                    exitCase.PendingWithIDEmployee = dto.IdEmployee;
                }

                exitCase.UpdatedBy = loggedInEmployeeId;
                exitCase.UpdatedAt = DateTime.Now;

                // 🔟 Insert History Record
                var history = new ExitCaseStatusHistory
                {
                    IdExitCase = exitCase.IdExitCase,
                    ActionType = dto.Action == "Approved" ? "MGR_APPROVE" : "MGR_REJECT",
                    FromStatus = "SUBMITTED",
                    ToStatus = dto.Action == "Approved" ? "MGR_APPROVE" : "MGR_REJECT",
                    PendingWith = dto.Action == "Approved" ? "HROFFICER" : "EMPLOYEE",
                    Remarks = dto.Remarks,
                    ApprovedLWD = dto.Action == "Approved" ? dto.ApprovedLWD : null,
                    CreatedBy = loggedInEmployeeId,
                    CreatedAt = DateTime.Now,
                    OrderNumber = newOrderNumber
                };

                await _dbContext.ExitCaseStatusHistory.AddAsync(history);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ReportingOfficerActionResponseDto
                {
                    Success = true,
                    IdExitCase = exitCase.IdExitCase,
                    Message = dto.Action == "Approved"
                        ? "Resignation approved and forwarded to HR."
                        : "Resignation rejected and sent back to employee."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Reporting Officer Action Failed");

                return new ReportingOfficerActionResponseDto
                {
                    Success = false,
                    IdExitCase = dto.IdExitCase,
                    Message = "Error occurred while processing action.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<bool> DeleteClearanceTemplateDepartment(int IdTemplateDept)
        {
            try
            {
                var existing = await _dbContext.ClearanceTemplateDepartments
                    .Where(x => x.IdTemplateDept == IdTemplateDept).FirstOrDefaultAsync();

                // Delete removed items
                _dbContext.ClearanceTemplateDepartments.Remove(existing);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ClearanceTemplateDepartments");
                throw;
            }
        }
        public async Task<HROfficerActionResponseDto> SubmitHROfficerActions(SubmitHROfficerActionsDto dto,int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // =========================
                // A) UPDATE EXIT CASE
                // =========================
                var exitCase = await _dbContext.ExitCases
                    .FirstOrDefaultAsync(x => x.IdExitCase == dto.IdExitCase);

                if (exitCase == null)
                {
                    return new HROfficerActionResponseDto
                    {
                        Success = false,
                        Message = "Exit case not found."
                    };
                }

                if (dto.ApprovedLWD.HasValue)
                    exitCase.ApprovedLWD = dto.ApprovedLWD;

                exitCase.ExitInterviewDate = dto.ExitInterviewDate;
                exitCase.ContactAfterExit = dto.ContactAfterExit;
                exitCase.IdClearanceTemplate = dto.IdClearanceTemplate;
                exitCase.AssignedClearanceTemplateBy = loggedInEmployeeId;
                exitCase.ClearanceInitiatedOn = DateTime.Now;
                exitCase.ExitStatus = "InClearance";
                exitCase.PendingWith = "HRMANAGER";
                exitCase.UpdatedBy = loggedInEmployeeId;
                exitCase.UpdatedAt = DateTime.Now;

                // =========================
                // B) CLEARANCE ASSIGNMENTS
                // =========================
                foreach (var dept in dto.ClearanceAssignments)
                {
                    ExitCaseClearanceAssignment assignment;

                    if (dept.IdExitCaseClearanceAssignment == 0)
                    {
                        assignment = new ExitCaseClearanceAssignment
                        {
                            IdExitCase = dto.IdExitCase,
                            IdDepartment = dept.IdDepartment,
                            IdClearanceTemplate = dto.IdClearanceTemplate,
                            IdAssigneeUser = dept.IdAssigneeUser,
                            DeptClearanceStatus = "PENDING",
                            AssignedBy = loggedInEmployeeId,
                            AssignedAt = DateTime.Now
                        };

                        await _dbContext.ExitCaseClearanceAssignments.AddAsync(assignment);
                    }
                    else
                    {
                        assignment = await _dbContext.ExitCaseClearanceAssignments
                            .FirstOrDefaultAsync(x =>
                                x.IdExitCaseClearanceAssignment == dept.IdExitCaseClearanceAssignment);

                        if (assignment == null) continue;

                        assignment.IdAssigneeUser = dept.IdAssigneeUser;
                    }

                    // =========================
                    // C) CLEARANCE LINES (SNAPSHOT)
                    // =========================
                    var templateLines = await _dbContext.ClearanceTemplateDepartments
                        .Where(x => x.IdTemplateDept == dept.IdTemplateDept)                        
                        .ToListAsync();

                    // Remove old snapshot if re-init
                    var existingLines = await _dbContext.ExitCaseDepartmentClearanceLines
                        .Where(x => x.IdExitCase == dto.IdExitCase
                                 && x.IdDepartment == dept.IdDepartment)
                        .ToListAsync();

                    _dbContext.ExitCaseDepartmentClearanceLines.RemoveRange(existingLines);

                    int sortOrder = 1;

                    // Checklist Rows
                    foreach (var line in templateLines)
                    {
                        await _dbContext.ExitCaseDepartmentClearanceLines.AddAsync(
                            new ExitCaseDepartmentClearanceLine
                            {
                                IdExitCase = dto.IdExitCase,
                                IdDepartment = dept.IdDepartment,
                                IdTemplateDept = dept.IdTemplateDept,
                                CheckListItem = line.CheckListItem,
                                DeptClearanceStatus = "PENDING",
                                SortOrder = sortOrder++,
                                CreatedBy = loggedInEmployeeId,
                                CreatedOn = DateTime.Now
                            });
                    }
                }

                // =========================
                // D) STATUS HISTORY
                // =========================
                var lastOrder = await _dbContext.ExitCaseStatusHistory
                    .Where(x => x.IdExitCase == dto.IdExitCase)
                    .OrderByDescending(x => x.OrderNumber)
                    .Select(x => x.OrderNumber)
                    .FirstOrDefaultAsync();

                await _dbContext.ExitCaseStatusHistory.AddAsync(
                    new ExitCaseStatusHistory
                    {
                        IdExitCase = dto.IdExitCase,
                        ActionType = "CLEARANCE_INITIATED",
                        FromStatus = "RepOfficerApproved",
                        ToStatus = "InClearance",
                        PendingWith = "CLEARANCE",
                        CreatedBy = loggedInEmployeeId,
                        CreatedAt = DateTime.Now,
                        OrderNumber = lastOrder + 1
                    });

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new HROfficerActionResponseDto
                {
                    Success = true,
                    IdExitCase = dto.IdExitCase,
                    Message = "Clearance process initiated successfully."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "HR Clearance Initiation Failed");

                return new HROfficerActionResponseDto
                {
                    Success = false,
                    Message = "Failed to initiate clearance.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        public async Task<HRManagerActionResponseDto> SubmitHRManagerActions(SubmitHRManagerActionsDto dto,int loggedInHRManagerId)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // A️⃣ PRE-CHECK – Clearance gating
                var pendingClearanceExists = await _dbContext.ExitCaseClearanceAssignments
                    .AnyAsync(x =>
                        x.IdExitCase == dto.IdExitCase &&
                        !new[] { "CLEARED", "NOT_APPLICABLE" }
                            .Contains(x.DeptClearanceStatus));

                if (pendingClearanceExists)
                {
                    return new HRManagerActionResponseDto
                    {
                        Success = false,
                        Message = "All departmental clearances must be completed before closure."
                    };
                }

                // B️⃣ UPDATE EXIT CASE
                var exitCase = await _dbContext.ExitCases
                    .FirstOrDefaultAsync(x => x.IdExitCase == dto.IdExitCase);

                if (exitCase == null)
                {
                    return new HRManagerActionResponseDto
                    {
                        Success = false,
                        Message = "Exit case not found."
                    };
                }

                var fromStatus = exitCase.ExitStatus;

                exitCase.ExitInterviewDate = dto.ExitInterviewDate;
               // exitCase.ExitInterviewDetails = dto.ExitInterviewDetails;
                exitCase.ExitStatus = "COMPLETED";
                exitCase.PendingWith = null;
                exitCase.UpdatedBy = loggedInHRManagerId;
                exitCase.UpdatedAt = DateTime.UtcNow;

                // C️⃣ INSERT STATUS HISTORY
                var lastOrder = await _dbContext.ExitCaseStatusHistory
                    .Where(x => x.IdExitCase == dto.IdExitCase)
                    .MaxAsync(x => (int?)x.OrderNumber) ?? 0;

                await _dbContext.ExitCaseStatusHistory.AddAsync(
                    new ExitCaseStatusHistory
                    {
                        IdExitCase = dto.IdExitCase,
                        ActionType = "CLOSURE_COMPLETED",
                        FromStatus = fromStatus,
                        ToStatus = "COMPLETED",
                        PendingWith = null,
                        Remarks = dto.ExitInterviewDetails,
                        CreatedBy = loggedInHRManagerId,
                        CreatedAt = DateTime.UtcNow,
                        OrderNumber = lastOrder + 1
                    });

                // D️⃣ UPDATE EMPLOYEE STATUS
                var employee = await _dbContext.Employees
                    .FirstOrDefaultAsync(x => x.IdEmployee == dto.IdEmployee);

                if (employee != null)
                {
                    employee.CurrentStatus = "NotWorking";
                    employee.LastWorkingDay = DateTime.UtcNow.Date;                    
                }

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return new HRManagerActionResponseDto
                {
                    Success = true,
                    IdExitCase = dto.IdExitCase,
                    Message = "Exit case closed successfully."
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error closing exit case {IdExitCase}", dto.IdExitCase);

                return new HRManagerActionResponseDto
                {
                    Success = false,
                    Message = "An error occurred while closing the exit case."
                };
            }
        }

        public async Task<GetExitClearanceDetailsResponseDto> GetExitClearanceDetails(
    int loggedInEmployeeId,
    int idExitCase,
    int? idDepartment = null,
    string? viewAsRole = null)
        {
            using var connection = _dbContext.Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();

            // 1) Exit case header
            var exitCase = await connection.QueryFirstOrDefaultAsync<ExitClearanceCaseHeaderDto>(
                @"SELECT 
            ec.IdExitCase,
            ec.CaseNumber,
            ec.IdEmployee,
            CONCAT(e.FirstName, ' ', COALESCE(e.MiddleName,''), ' ', e.LastName) AS EmployeeName,
            ec.ExitStatus,
            ec.PendingWith,
            ec.InitiationDate,
            ec.ProposedLWD,
            ec.ApprovedLWD,
            ec.IdClearanceTemplate,
            ec.ClearanceInitiatedOn
          FROM ExitCases ec
          INNER JOIN Employees e ON e.IdEmployee = ec.IdEmployee
          WHERE ec.IdExitCase = @IdExitCase",
                new { IdExitCase = idExitCase });

            if (exitCase == null)
                return new GetExitClearanceDetailsResponseDto { Success = false, Message = "Invalid Exit Case." };

            // 2) Role derivation (replace with your real RBAC if you have)
            bool isEmployee = exitCase.IdEmployee == loggedInEmployeeId;

            // HR detection by DepartmentCode (same style as your existing code)
            bool isHr = false;
            var hrDepartmentCode = _configuration["Departments:HRCode"] ?? "HRD";

            var loggedInDeptCode = await connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT d.DepartmentCode
          FROM Employees e
          INNER JOIN Departments d ON d.IdDepartment = e.IdDepartment
          WHERE e.IdEmployee = @EmpId",
                new { EmpId = loggedInEmployeeId });

            if (!string.IsNullOrEmpty(loggedInDeptCode) &&
                loggedInDeptCode.Equals(hrDepartmentCode, StringComparison.OrdinalIgnoreCase))
                isHr = true;

            // 3) Assignments (filtered by optional dept)
            var assignmentsSql = @"
        SELECT
            a.IdExitCase,
            a.IdDepartment,
            d.DepartmentName,
            a.IdAssigneeUser,
            CONCAT(u.FirstName, ' ', COALESCE(u.MiddleName,''), ' ', u.LastName) AS AssigneeName,
            a.DeptClearanceStatus,
            a.AssignedAt
        FROM ExitCaseClearanceAssignments a
        INNER JOIN Departments d ON d.IdDepartment = a.IdDepartment
        LEFT JOIN Employees u ON u.IdEmployee = a.IdAssigneeUser
        WHERE a.IdExitCase = @IdExitCase
    ";

            if (idDepartment.HasValue)
                assignmentsSql += " AND a.IdDepartment = @IdDepartment ";

            var assignments = (await connection.QueryAsync<AssignmentViewDto>(
                assignmentsSql,
                new { IdExitCase = idExitCase, IdDepartment = idDepartment }
            )).ToList();

            // 4) Dept clearer authorization
            bool isDeptClearer = false;
            if (idDepartment.HasValue)
                isDeptClearer = assignments.Any(a => a.IdAssigneeUser == loggedInEmployeeId);

            // 5) Enforce access
            if (!isEmployee && !isHr)
            {
                if (!idDepartment.HasValue || !isDeptClearer)
                    return new GetExitClearanceDetailsResponseDto { Success = false, Message = "Unauthorized / Not Assigned." };
            }

            if (idDepartment.HasValue && !assignments.Any())
                return new GetExitClearanceDetailsResponseDto { Success = false, Message = "Invalid Department / No assignment." };

            var deptIds = assignments.Select(a => a.IdDepartment).Distinct().ToList();

            // 6) Load lines including header metadata
            var lines = new List<LineViewDto>();
            if (deptIds.Any())
            {
                lines = (await connection.QueryAsync<LineViewDto>(
                    @"SELECT
                l.IdExitCaseDepartmentClearanceLine,
                l.IdExitCase,
                l.IdDepartment,
                d.DepartmentName,
                l.CheckListItem,
                l.DeptClearanceStatus,
                l.DeptRemarks,
                l.ClearedBy,
                CONCAT(cb.FirstName, ' ', COALESCE(cb.MiddleName,''), ' ', cb.LastName) AS ClearedByName,
                l.ClearedAt,
                l.SortOrder
              FROM ExitCaseDepartmentClearanceLines l
              INNER JOIN Departments d ON d.IdDepartment = l.IdDepartment
              LEFT JOIN Employees cb ON cb.IdEmployee = l.ClearedBy
              WHERE l.IdExitCase = @IdExitCase
                AND l.IdDepartment IN @DeptIds
              ORDER BY l.IdDepartment, l.SortOrder",
                    new { IdExitCase = idExitCase, DeptIds = deptIds }
                )).ToList();
            }

            // 7) Build response blocks
            var lineByDept = lines.GroupBy(x => x.IdDepartment).ToDictionary(g => g.Key, g => g.ToList());

            var blocks = new List<ExitClearanceDepartmentBlockDto>();

            foreach (var a in assignments)
            {
                var deptLines = lineByDept.GetValueOrDefault(a.IdDepartment, new List<LineViewDto>());

                // header row definition:
                // - SortOrder == 0 OR CheckListItem is NULL
                var headerLine = deptLines
                    .OrderBy(x => x.SortOrder ?? int.MaxValue)
                    .FirstOrDefault(x => (x.SortOrder ?? 999999) == 0 || string.IsNullOrEmpty(x.CheckListItem));

                var checklist = deptLines
                    .Where(x => !string.IsNullOrEmpty(x.CheckListItem) && (x.SortOrder ?? 0) > 0)
                    .OrderBy(x => x.SortOrder ?? 999999)
                    .Select(x => new ExitClearanceChecklistItemDto
                    {
                        IdExitCaseDepartmentClearanceLine = x.IdExitCaseDepartmentClearanceLine,
                        CheckListItem = x.CheckListItem,
                        DeptClearanceStatus = x.DeptClearanceStatus,
                        SortOrder = x.SortOrder
                    })
                    .ToList();

                bool canEdit = (a.IdAssigneeUser == loggedInEmployeeId) && !isHr; // HR read-only per your rule

                blocks.Add(new ExitClearanceDepartmentBlockDto
                {
                    IdDepartment = a.IdDepartment,
                    DepartmentName = a.DepartmentName,
                    IdAssigneeUser = a.IdAssigneeUser,
                    AssigneeName = a.AssigneeName,
                    DeptClearanceStatus = a.DeptClearanceStatus,
                    AssignedAt = a.AssignedAt,

                    CanEditChecklist = canEdit,
                    CanEditDeptHeader = canEdit,

                    Header = new DeptHeaderDto
                    {
                        DeptClearanceStatus = headerLine?.DeptClearanceStatus ?? a.DeptClearanceStatus,
                        DeptRemarks = headerLine?.DeptRemarks,
                        ClearedBy = headerLine?.ClearedBy,
                        ClearedByName = headerLine?.ClearedByName,
                        ClearedAt = headerLine?.ClearedAt
                    },

                    Checklist = checklist
                });
            }

            return new GetExitClearanceDetailsResponseDto
            {
                Success = true,
                Message = "Exit clearance details retrieved successfully.",
                ExitCase = exitCase,
                Departments = blocks
            };
        }

        public async Task<SubmitExitCaseDepartmentClearanceLinesResponseDto> SubmitExitCaseDepartmentClearanceLines(SubmitExitCaseDepartmentClearanceLinesDto dto,int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // A1) Validate exit case exists
                var exitCase = await _dbContext.ExitCases.FirstOrDefaultAsync(x => x.IdExitCase == dto.IdExitCase);
                if (exitCase == null)
                    return Fail(dto, "Invalid Case.");

                // A2) Validate department header row exists (SortOrder = 0 OR CheckListItem is null)
                var headerRow = await _dbContext.ExitCaseDepartmentClearanceLines
                    .FirstOrDefaultAsync(x =>
                        x.IdExitCase == dto.IdExitCase &&
                        x.IdDepartment == dto.IdDepartment );

                if (headerRow == null)
                    return Fail(dto, "Invalid Department / Clearance not initiated.");

                // A3) Authorize: must be assigned user for this department
                var assignment = await _dbContext.ExitCaseClearanceAssignments.FirstOrDefaultAsync(x =>
                    x.IdExitCase == dto.IdExitCase &&
                    x.IdDepartment == dto.IdDepartment);

                if (assignment == null)
                    return Fail(dto, "Not Assigned / Department not part of clearance.");

                if (assignment.IdAssigneeUser != loggedInEmployeeId)
                    return Fail(dto, "Unauthorized / Forbidden. You are not assigned to this department.");

                // B) Update checklist rows
                if (dto.ClearanceLineUpdates != null && dto.ClearanceLineUpdates.Any())
                {
                    foreach (var upd in dto.ClearanceLineUpdates)
                    {
                        var line = await _dbContext.ExitCaseDepartmentClearanceLines
                            .FirstOrDefaultAsync(x => x.IdExitCaseDepartmentClearanceLine == upd.IdExitCaseDepartmentClearanceLine);

                        if (line == null)
                            return Fail(dto, $"Invalid clearance line: {upd.IdExitCaseDepartmentClearanceLine}");

                        // Ensure it belongs to the same case/department
                        if (line.IdExitCase != dto.IdExitCase || line.IdDepartment != dto.IdDepartment)
                            return Fail(dto, $"Line does not belong to this case/department: {upd.IdExitCaseDepartmentClearanceLine}");

                        // Prevent editing header row
                        if ((line.SortOrder ?? 0) == 0 || string.IsNullOrEmpty(line.CheckListItem))
                            return Fail(dto, $"Header row cannot be updated as checklist item: {upd.IdExitCaseDepartmentClearanceLine}");

                        // No IsCompleted column -> map to status
                        line.DeptClearanceStatus = upd.IsCompleted ? "CLEARED" : "PENDING";
                        line.UpdatedBy = loggedInEmployeeId;
                        line.UpdatedAt = DateTime.Now;
                    }
                }

                // C) Update department header row fields
                if (dto.DeptRemarks != null)
                {
                    headerRow.DeptRemarks = dto.DeptRemarks;
                    headerRow.UpdatedBy = loggedInEmployeeId;
                    headerRow.UpdatedAt = DateTime.Now;
                }

                // ⚠️ DueAmount not in DB. If you add column later, update it here.
                // headerRow.DueAmount = dto.DueAmount;

                if (!string.IsNullOrEmpty(dto.DeptClearanceStatus))
                {
                    var normalized = NormalizeDeptStatus(dto.DeptClearanceStatus);
                    if (normalized == null)
                        return Fail(dto, "Invalid DeptClearanceStatus. Allowed: Pending, Cleared, Not Applicable");

                    headerRow.DeptClearanceStatus = normalized;
                    assignment.DeptClearanceStatus = normalized;
                    headerRow.UpdatedBy = loggedInEmployeeId;
                    headerRow.UpdatedAt = DateTime.Now;
                }

                // D) Completion rule
                bool wantsCleared =
                    (dto.MarkDepartmentCleared == true) ||
                    (NormalizeDeptStatus(dto.DeptClearanceStatus) == "CLEARED");

                if (wantsCleared)
                {
                    // check all checklist lines cleared (recommended rule)
                    var checklistLines = await _dbContext.ExitCaseDepartmentClearanceLines
                        .Where(x => x.IdExitCase == dto.IdExitCase
                                 && x.IdDepartment == dto.IdDepartment
                                 && (x.SortOrder ?? 0) > 0
                                 && x.CheckListItem != null)
                        .ToListAsync();

                    // If you later add Mandatory flag, check only mandatory.
                    bool allCompleted = checklistLines.All(x => (x.DeptClearanceStatus ?? "").ToUpper() == "CLEARED");

                    if (!allCompleted)
                        return Fail(dto, "Cannot mark Cleared. All checklist items must be completed.");

                    headerRow.DeptClearanceStatus = "CLEARED";
                    assignment.DeptClearanceStatus = "CLEARED";
                    headerRow.ClearedBy = loggedInEmployeeId;
                    headerRow.ClearedAt = DateTime.Now;
                    headerRow.UpdatedBy = loggedInEmployeeId;
                    headerRow.UpdatedAt = DateTime.Now;
                }

                // E) Case-level progression (all departments completed)
                // Completed = CLEARED or NOT_APPLICABLE
                var deptStatuses = await _dbContext.ExitCaseClearanceAssignments
                    .Where(x => x.IdExitCase == dto.IdExitCase)
                    .Select(x => x.DeptClearanceStatus)
                    .ToListAsync();

                bool allDone = deptStatuses.Any() &&
                               deptStatuses.All(s =>
                               {
                                   var st = (s ?? "").ToUpper();
                                   return st == "CLEARED" || st == "NOT_APPLICABLE";
                               });

                if (allDone)
                {
                    exitCase.ExitStatus = "ReadyForClosure";
                    exitCase.PendingWith = "HRMANAGER";
                    exitCase.UpdatedBy = loggedInEmployeeId;
                    exitCase.UpdatedAt = DateTime.Now;
                }

                // F) Status history
                string actionType = wantsCleared ? "DEPT_CLEARED" : "DEPT_CLEARANCE_UPDATED";

                var lastOrder = await _dbContext.ExitCaseStatusHistory
                    .Where(x => x.IdExitCase == dto.IdExitCase)
                    .OrderByDescending(x => x.OrderNumber)
                    .Select(x => x.OrderNumber)
                    .FirstOrDefaultAsync();

                await _dbContext.ExitCaseStatusHistory.AddAsync(new ExitCaseStatusHistory
                {
                    IdExitCase = dto.IdExitCase,
                    ActionType = actionType,
                    FromStatus = exitCase.ExitStatus,        // you can store previous before change if needed
                    ToStatus = exitCase.ExitStatus,
                    PendingWith = exitCase.PendingWith,
                    CreatedBy = loggedInEmployeeId,
                    CreatedAt = DateTime.Now,
                    OrderNumber = lastOrder + 1
                });

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Build response (return updated snapshot)
                var updatedLines = await _dbContext.ExitCaseDepartmentClearanceLines
                    .Where(x => x.IdExitCase == dto.IdExitCase
                             && x.IdDepartment == dto.IdDepartment
                             && (x.SortOrder ?? 0) > 0
                             && x.CheckListItem != null)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                return new SubmitExitCaseDepartmentClearanceLinesResponseDto
                {
                    Success = true,
                    Message = "Department clearance updated successfully.",
                    IdExitCase = dto.IdExitCase,
                    IdDepartment = dto.IdDepartment,
                    DepartmentStatus = headerRow.DeptClearanceStatus,
                    DeptRemarks = headerRow.DeptRemarks,
                    DueAmount = dto.DueAmount, // echo back; real value only if you add column
                    ClearedBy = headerRow.ClearedBy,
                    ClearedAt = headerRow.ClearedAt,
                    CaseExitStatus = exitCase.ExitStatus,
                    CasePendingWith = exitCase.PendingWith,
                    Checklist = updatedLines.Select(l => new UpdatedChecklistItemDto
                    {
                        IdExitCaseDepartmentClearanceLine = l.IdExitCaseDepartmentClearanceLine,
                        CheckListItem = l.CheckListItem,
                        IsCompleted = (l.DeptClearanceStatus ?? "").ToUpper() == "CLEARED",
                        LineStatus = l.DeptClearanceStatus
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "SubmitExitCaseDepartmentClearanceLines failed");
                return Fail(dto, "Failed to update clearance.", ex.Message);
            }
        }

        private static SubmitExitCaseDepartmentClearanceLinesResponseDto Fail(
            SubmitExitCaseDepartmentClearanceLinesDto dto,
            string message,
            string? error = null)
        {
            var resp = new SubmitExitCaseDepartmentClearanceLinesResponseDto
            {
                Success = false,
                Message = message,
                IdExitCase = dto.IdExitCase,
                IdDepartment = dto.IdDepartment
            };

            if (!string.IsNullOrEmpty(error))
                resp.Errors.Add(error);

            return resp;
        }

        private static string? NormalizeDeptStatus(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var s = input.Trim().ToUpper();

            if (s == "PENDING" || s == "PEND") return "PENDING";
            if (s == "CLEARED" || s == "CLEAR") return "CLEARED";
            if (s == "NOT APPLICABLE" || s == "NOT_APPLICABLE" || s == "NA") return "NOT_APPLICABLE";

            return null;
        }




        // Internal view DTOs for queries
        private class AssignmentViewDto
        {
            public int IdExitCase { get; set; }
            public int IdDepartment { get; set; }
            public string DepartmentName { get; set; }
            public int IdAssigneeUser { get; set; }
            public string AssigneeName { get; set; }
            public string DeptClearanceStatus { get; set; }
            public DateTime AssignedAt { get; set; }
        }

        private class LineViewDto
        {
            public int IdExitCaseDepartmentClearanceLine { get; set; }
            public int IdExitCase { get; set; }
            public int IdDepartment { get; set; }
            public string DepartmentName { get; set; }
            public string? CheckListItem { get; set; }
            public string? DeptClearanceStatus { get; set; }
            public string? DeptRemarks { get; set; }
            public int? ClearedBy { get; set; }
            public string? ClearedByName { get; set; }
            public DateTime? ClearedAt { get; set; }
            public int? SortOrder { get; set; }
        }



        #endregion
    }
}

