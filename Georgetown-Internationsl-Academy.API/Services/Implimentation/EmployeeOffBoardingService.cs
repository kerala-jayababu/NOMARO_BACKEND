using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;
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

        public async Task<IEnumerable<ResignationRequestDto>> GetResignationRequests(int idLoggedInEmployee, string? roleType = null, int? idEmployee = null, DateTime? initiationDate = null)
        {
            try
            {
                // Validate inputs
                if (idEmployee <= 0 && string.IsNullOrEmpty(roleType))
                {
                    throw new ArgumentException("If IdEmployee is not provided, RoleType is mandatory. RoleType values: REPOFFICER, HREXECUTIVE, HRHEAD");
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
                        // Get all resignation requests where logged-in employee is the reporting officer
                        query.Append(" AND ec.PendingWithIDEmployee = @LoggedInEmployeeId ");
                        parameters.Add("LoggedInEmployeeId", idLoggedInEmployee);
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
                OR ec.PendingWith IN ('HREXECUTIVE', 'HRHEAD')
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

                        var result = await connection.QueryAsync<ResignationRequestDto>(query.ToString(), parameters);
                        return result;
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

        #endregion
    }
}

