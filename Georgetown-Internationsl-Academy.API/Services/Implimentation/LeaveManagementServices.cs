using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Ocsp;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;


namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class LeaveManaementServices : ILeaveManaementServices
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<NotificationConfigService> _logger;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;

        public LeaveManaementServices(IConfiguration configuration, ApplicationDBContext dbContext, IApprovalWorkflowService approveWorkflowService, ILogger<NotificationConfigService> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
            _approvalWorkflowService = approveWorkflowService;
        }
        #region LeaveTypes

        public async Task<IEnumerable<LeaveTypesDto>> GetLeaveTypes()
        {
            try
            {
                var leaveTypes = await _dbContext.LeaveTypes
                    .OrderBy(x => x.LeaveTypeName.ToUpper())
                    .Select(x => new LeaveTypesDto
                    {
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        LeaveCode = x.LeaveCode
                    })
                    .ToListAsync();

                // ✅ If no data return empty list (not null)
                return leaveTypes ?? new List<LeaveTypesDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Types.");
                throw;
            }
        }

        public async Task<IEnumerable<LeaveTypesDto>> GetLeaveTypesEmployee(int idEmployee, int idYear)
        {
            try
            {
                var query =
              from lt in _dbContext.LeaveTypes
              join cfg in _dbContext.EmployeeLeaveConfigDetails
                  on lt.IdLeaveType equals cfg.IdLeaveType
              join hdr in _dbContext.EmployeeLeaveConfigs
                  on cfg.IdEmployeeLeaveConfig equals hdr.IdEmployeeLeaveConfig
              where hdr.IdEmployee == idEmployee
                    && cfg.IdYear == idYear orderby lt.LeaveTypeName.ToUpper()
              select new LeaveTypesDto
              {
                  IdLeaveType = lt.IdLeaveType,
                  LeaveTypeName = lt.LeaveTypeName,
                  LeaveCode = lt.LeaveCode
              };

                var leaveTypes = await query.Distinct().ToListAsync();
                return leaveTypes ?? new List<LeaveTypesDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Types.");
                throw;
            }
        }

        public async Task<IEnumerable<LeaveTypesDto>> GetLeaveTypesOfEmployee(int idYear, int idEmployee)
        {
            try
            {
                var query =
                    from lt in _dbContext.LeaveTypes
                    join cd in _dbContext.EmployeeLeaveConfigDetails
                        on lt.IdLeaveType equals cd.IdLeaveType
                    join ch in _dbContext.EmployeeLeaveConfigs
                        on cd.IdEmployeeLeaveConfig equals ch.IdEmployeeLeaveConfig
                    where ch.IdEmployee == idEmployee
                          && cd.IdYear == idYear
                    orderby lt.LeaveTypeName
                    select new LeaveTypesDto
                    {
                        IdLeaveType = lt.IdLeaveType,
                        LeaveTypeName = lt.LeaveTypeName,
                        LeaveCode = lt.LeaveCode
                    };

                var leaveTypes = await query
                    .Distinct()     // in case multiple config rows point to same leave type
                    .ToListAsync();

                return leaveTypes;   // ToListAsync never returns null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Types for employee {IdEmployee}, year {IdYear}.",
                    idEmployee, idYear);
                throw;
            }
        }


        public async Task<bool> AddOrUpdateLeaveTypes(LeaveTypesDto leaveTypedto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var conflicts = await _dbContext.LeaveTypes
                    .Where(t => t.LeaveCode.ToUpper() == leaveTypedto.LeaveCode.ToUpper() || t.LeaveTypeName.ToUpper() == leaveTypedto.LeaveTypeName.ToUpper())
                    .Select(db => new
                    {
                        db.IdLeaveType,
                        Code = db.LeaveCode.Trim().ToUpper(),
                        Name = db.LeaveTypeName.Trim().ToUpper()
                    })
                    .ToListAsync();

                var errors = new List<string>();

                if (conflicts.Any())
                    errors.Add($"LeaveCode or Name already exists");


                if (string.IsNullOrWhiteSpace(leaveTypedto.LeaveCode))
                    throw new ArgumentException("LeaveCode is required.");

                if (string.IsNullOrWhiteSpace(leaveTypedto.LeaveTypeName))
                    throw new ArgumentException("LeaveTypeName is required.");

                // ✅ normalize
                leaveTypedto.LeaveCode = leaveTypedto.LeaveCode.Trim().ToUpper();
                leaveTypedto.LeaveTypeName = leaveTypedto.LeaveTypeName.Trim();

                // ✅ Update
                if (leaveTypedto.IdLeaveType > 0)
                {
                    var existing = await _dbContext.LeaveTypes
                        .FirstOrDefaultAsync(x => x.IdLeaveType == leaveTypedto.IdLeaveType);

                    if (existing == null)
                        throw new ArgumentException($"LeaveType not found. IdLeaveType = {leaveTypedto.IdLeaveType}");

                    // ✅ Uniqueness check in DB (LeaveCode)
                    bool codeExists = await _dbContext.LeaveTypes.AnyAsync(x =>
                        x.LeaveCode == leaveTypedto.LeaveCode &&
                        x.IdLeaveType != leaveTypedto.IdLeaveType);

                    if (codeExists)
                        throw new ArgumentException($"LeaveCode '{leaveTypedto.LeaveCode}' already exists.");

                    // ✅ Uniqueness check in DB (LeaveTypeName)
                    bool nameExists = await _dbContext.LeaveTypes.AnyAsync(x =>
                        x.LeaveTypeName == leaveTypedto.LeaveTypeName &&
                        x.IdLeaveType != leaveTypedto.IdLeaveType);

                    if (nameExists)
                        throw new ArgumentException($"LeaveTypeName '{leaveTypedto.LeaveTypeName}' already exists.");

                    existing.LeaveCode = leaveTypedto.LeaveCode;
                    existing.LeaveTypeName = leaveTypedto.LeaveTypeName;

                    // ✅ if your LeaveTypes table has UpdatedAt/UpdatedBy columns
                    // existing.UpdatedAt = DateTime.Now;
                    // existing.UpdatedBy = loggedInEmployeeId;

                    _dbContext.LeaveTypes.Update(existing);
                }
                else
                {
                    // ✅ Insert uniqueness check
                    bool codeExists = await _dbContext.LeaveTypes.AnyAsync(x => x.LeaveCode == leaveTypedto.LeaveCode);
                    if (codeExists)
                        throw new ArgumentException($"LeaveCode '{leaveTypedto.LeaveCode}' already exists.");

                    bool nameExists = await _dbContext.LeaveTypes.AnyAsync(x => x.LeaveTypeName == leaveTypedto.LeaveTypeName);
                    if (nameExists)
                        throw new ArgumentException($"LeaveTypeName '{leaveTypedto.LeaveTypeName}' already exists.");

                    var newLeaveType = new LeaveTypes
                    {
                        LeaveCode = leaveTypedto.LeaveCode,
                        LeaveTypeName = leaveTypedto.LeaveTypeName,
                    };

                    // ✅ if your LeaveTypes table has CreatedAt/CreatedBy columns
                    // newLeaveType.CreatedAt = DateTime.Now;
                    // newLeaveType.CreatedBy = loggedInEmployeeId;

                    await _dbContext.LeaveTypes.AddAsync(newLeaveType);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating leave types.");
                throw;
            }
        }


        #endregion

        #region LeaveTemplates
        public async Task<IEnumerable<LeaveTemplateDto>> GetLeaveTemplates(int? IdYear, string Status, string? searchText = null)
        {
            try
            {
                var query = _dbContext.LeaveTemplates.AsQueryable();

                if (IdYear.HasValue)
                    query = query.Where(x => x.IdYear == IdYear.Value);
                if (Status.ToUpper() != "ALL")
                    query = query.Where(x => x.ApprovlStatus == Status);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var text = searchText.Trim();
                    query = query.Where(x =>
                        x.LeaveTemplateName.Contains(text) ||
                        (x.LeaveTemplateDesc != null && x.LeaveTemplateDesc.Contains(text)));
                }

                var result = await query
                    .OrderBy(x => x.LeaveTemplateName)
                    .Select(t => new LeaveTemplateDto
                    {
                        IdLeaveTemplate = t.IdLeaveTemplate,
                        LeaveTemplateName = t.LeaveTemplateName,
                        ApprovlStatus = t.ApprovlStatus,
                        LeaveTemplateDesc = t.LeaveTemplateDesc,
                        IdYear = t.IdYear,
                        CreatedBy = t.CreatedBy,
                        CreatedAt = t.CreatedAt,
                        UpdatedBy = t.UpdatedBy,
                        UpdatedAt = t.UpdatedAt,
                        IdApprovedBy = t.IdApprovedBy
                    })
                    .ToListAsync();

                return result ?? new List<LeaveTemplateDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Templates.");
                throw;
            }
        }

        public async Task<LeaveTemplateDto> GetLeaveTemplateByID(int idLeaveTemplate)
        {
            try
            {
                var header = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == idLeaveTemplate);

                if (header == null)
                    throw new ArgumentException("Leave template not found.");

                // ✅ Load details
                var details = await _dbContext.LeaveTemplateDetails
                    .Where(x => x.IdLeaveTemplate == idLeaveTemplate)
                    .OrderBy(x => x.IdLeaveTemplateDetails)
                    .Select(x => new LeaveTemplateDetailsDto
                    {
                        IdLeaveTemplateDetails = x.IdLeaveTemplateDetails,
                        IdLeaveTemplate = x.IdLeaveTemplate,
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        LeaveCode = x.LeaveCode,
                        IdYear = x.IdYear,

                        EffectiveFrom = x.EffectiveFrom,
                        EffectiveTo = x.EffectiveTo,

                        ApplicableGender = x.ApplicableGender,
                        IsPaid = x.IsPaid,
                        SalaryDeductionPercent = x.SalaryDeductionPercent,
                        SalaryDeductAfterDays = x.SalaryDeductAfterDays,
                        AllowHalfDay = x.AllowHalfDay,
                        RequiresApproval = x.RequiresApproval,
                        RequiredApprovalLevel = x.RequiredApprovalLevel,

                        RequiresDocument = x.RequiresDocument,
                        DocumentRequiredAfterDays = x.DocumentRequiredAfterDays,

                        IsCarryForwardAllowed = x.IsCarryForwardAllowed,
                        MaxCarryForwardDays = x.MaxCarryForwardDays,

                        IncludeHolidaysBetween = x.IncludeHolidaysBetween,

                        MaxLeavesPerYear = x.MaxLeavesPerYear,
                        MaxLeavesPerMonth = x.MaxLeavesPerMonth,

                        AllowBackdatedLeave = x.AllowBackdatedLeave,
                        BackdateLimitDays = x.BackdateLimitDays
                    }).ToListAsync();

                foreach (var det in details)
                {
                    det.leaveWorkFlowDetails = await
                        (
                            from wfd in _dbContext.WorkFlowConfigDetails
                            join wf in _dbContext.WorkFlowConfig
                                on wfd.IdWorkFlowConfig equals wf.IdWorkFlowConfig
                            where wf.EntityCode == "LEAVE_" + det.IdLeaveTemplateDetails.ToString()
                            select new LeaveWorkFlowDetailDto
                            {
                                IdWorkFlowConfigDetail = wfd.IdWorkFlowConfigDetail,
                                IdWorkFlowConfig = wfd.IdWorkFlowConfig,
                                LevelNumber = wfd.LevelNumber,
                                ApprovalAuthorityType = wfd.ApprovalAuthorityType,
                                ApprovalStatusName = wfd.ApprovalStatusName,
                                ApprovalAuthorityID = wfd.ApprovalAuthorityID
                            }
                        ).ToListAsync();

                }

                return new LeaveTemplateDto
                {
                    IdLeaveTemplate = header.IdLeaveTemplate,
                    LeaveTemplateName = header.LeaveTemplateName,
                    LeaveTemplateDesc = header.LeaveTemplateDesc,
                    ApprovlStatus = header.ApprovlStatus,
                    IdApprovedBy = header.IdApprovedBy,
                    IdYear = header.IdYear,
                    CreatedBy = header.CreatedBy,
                    CreatedAt = header.CreatedAt,
                    UpdatedBy = header.UpdatedBy,
                    UpdatedAt = header.UpdatedAt,
                    LeaveTemplateDetails = details
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching LeaveTemplate. IdLeaveTemplate: {Id}", idLeaveTemplate);
                throw;
            }
        }

        public async Task<bool> AddUpdateLeaveTemplate(LeaveTemplatePostDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.LeaveTemplateName))
                    throw new ArgumentException("LeaveTemplateName is required.");

                dto.LeaveTemplateName = dto.LeaveTemplateName.Trim();

                // ✅ (1) Validate template name uniqueness
                bool nameExists = await _dbContext.LeaveTemplates.AnyAsync(x =>
                    x.LeaveTemplateName == dto.LeaveTemplateName &&
                    x.IdLeaveTemplate != dto.IdLeaveTemplate);

                if (nameExists)
                    throw new ArgumentException($"LeaveTemplateName '{dto.LeaveTemplateName}' already exists.");

                LeaveTemplates headerEntity;
                if (dto.IdLeaveTemplate > 0)
                {
                    headerEntity = await _dbContext.LeaveTemplates
                        .FirstOrDefaultAsync(x => x.IdLeaveTemplate == dto.IdLeaveTemplate);

                    if (headerEntity == null)
                        throw new ArgumentException("Leave template not found.");

                    headerEntity.LeaveTemplateName = dto.LeaveTemplateName;
                    headerEntity.LeaveTemplateDesc = dto.LeaveTemplateDesc;
                    headerEntity.IdYear = dto.IdYear;
                    headerEntity.UpdatedAt = DateTime.Now;
                    headerEntity.UpdatedBy = loggedInEmployeeId;
                    headerEntity.ApprovlStatus = dto.ApprovlStatus;
                    _dbContext.LeaveTemplates.Update(headerEntity);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    headerEntity = new LeaveTemplates
                    {
                        LeaveTemplateName = dto.LeaveTemplateName,
                        LeaveTemplateDesc = dto.LeaveTemplateDesc,
                        IdYear = dto.IdYear,
                        ApprovlStatus = dto.ApprovlStatus,
                        CreatedAt = DateTime.Now,
                        CreatedBy = loggedInEmployeeId
                    };
                    await _dbContext.LeaveTemplates.AddAsync(headerEntity);
                    await _dbContext.SaveChangesAsync();

                }
                await transaction.CommitAsync();
                //await SubmitLeaveTemplateForApproval(headerEntity.IdLeaveTemplate, loggedInEmployeeId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving Leave Template.");
                throw;
            }
        }

        public async Task<bool> DeleteLeaveTemplateDetail(int idLeaveTemplateDetail)
        {
            // 1. Check dependency in EmployeeLeaveConfigDetails
            bool isUsed = _dbContext.EmployeeLeaveConfigDetails
                .Any(x =>
                    x.IdLeaveTemplateDetail == idLeaveTemplateDetail);

            if (isUsed)
            {
                throw new ArgumentException(
                    "This leave template detail is already mapped to an employee configuration and cannot be deleted."
                );
            }

            // 2. Fetch the template detail
            var entity = _dbContext.LeaveTemplateDetails
                .FirstOrDefault(x => x.IdLeaveTemplateDetails == idLeaveTemplateDetail);

            if (entity == null)
                return false;

            // 3. Delete
            _dbContext.LeaveTemplateDetails.Remove(entity);
            _dbContext.SaveChanges();
            return true;
        }

        public async Task<List<EmployeeNameList>> GetEmployeesNotConfiguredLeave(int idWorkYear)
        {
            var employees = await _dbContext.Employees
                .Where(e =>
                    !_dbContext.EmployeeLeaveConfigs
                        .Join(_dbContext.LeaveTemplates,
                              elc => elc.IdLeaveTemplate,
                              lt => lt.IdLeaveTemplate,
                              (elc, lt) => new { elc, lt })
                        .Any(x =>
                            x.elc.IdEmployee == e.IdEmployee &&
                            x.lt.IdYear == idWorkYear)
                )
                .Select(e => new EmployeeNameList
                {
                    Idemployee = e.IdEmployee.Value,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName =
                        (e.FirstName ?? string.Empty) +
                        (string.IsNullOrEmpty(e.LastName) ? string.Empty : " " + e.LastName)
                })
                .AsNoTracking().OrderBy(ee => ee.EmployeeName)
                .ToListAsync();

            return employees;
        }

        public async Task<List<EmpLeaveConfigDetailsDto>> GetEmployeesLeaveConfigStatusDetails(int idWorkYear, int? IdDepartment, int? IdDesignation)
        {
            var query = _dbContext.Employees.AsQueryable();

            // Apply conditional filters
            if (IdDepartment.HasValue)
            {
                if (IdDepartment.Value > 0)
                    query = query.Where(e => e.IdDepartment == IdDepartment.Value);
            }

            if (IdDesignation.HasValue)
            {
                if (IdDesignation.Value > 0)
                    query = query.Where(e => e.IdDesignation == IdDesignation.Value);
            }

            var result = await query
                .Select(e => new EmpLeaveConfigDetailsDto
                {
                    IdEmployee = e.IdEmployee.Value,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName =
                        (e.FirstName ?? "") +
                        (string.IsNullOrEmpty(e.LastName) ? "" : " " + e.LastName),

                    JoiningDate = e.JoiningDate,
                    Email = e.EmailID,

                    DesignationName = _dbContext.Designations
                        .Where(d => d.IdDesignation == e.IdDesignation)
                        .Select(d => d.DesignationName)
                        .FirstOrDefault(),

                    DepartmentName = _dbContext.Departments
                        .Where(d => d.IdDepartment == e.IdDepartment)
                        .Select(d => d.DepartmentName)
                        .FirstOrDefault(),

                    IsConfigured = _dbContext.EmployeeLeaveConfigs
                        .Join(_dbContext.LeaveTemplates,
                            elc => elc.IdLeaveTemplate,
                            lt => lt.IdLeaveTemplate,
                            (elc, lt) => new { elc, lt })
                        .Any(x =>
                            x.elc.IdEmployee == e.IdEmployee &&
                            x.lt.IdYear == idWorkYear)
                })
                .AsNoTracking()
                .OrderBy(x => x.EmployeeName)
                .ToListAsync();

            return result;
        }

        public async Task<bool> SubmitLeaveTemplateForApproval(int idLeaveTemplate, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (idLeaveTemplate <= 0)
                    throw new ArgumentException("IdLeaveTemplate is required.");

                var template = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == idLeaveTemplate);

                if (template == null)
                    throw new ArgumentException("Leave template not found.");

                if (template.ApprovlStatus == "APPROVED")
                    throw new ArgumentException("Approved template cannot be resubmitted/rejected.");

                // ✅ Update approval status
                template.ApprovlStatus = "SUBMITTED";
                template.UpdatedBy = loggedInEmployeeId;
                template.UpdatedAt = DateTime.Now;

                _dbContext.LeaveTemplates.Update(template);
                await _dbContext.SaveChangesAsync();

                // ✅ Initiate workflow for each record
                var entityCode = _configuration["WorkflowEntityCodes:LeaveTemplate"];

                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    idLeaveTemplate, entityCode, loggedInEmployeeId, "SUBMITTED", null, null);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error submitting LeaveTemplate for approval. IdLeaveTemplate={Id}",
                    idLeaveTemplate);
                throw;
            }
        }


        public async Task<bool> ApproveLeaveTemplate(int idLeaveTemplate, string approvalStatus, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (idLeaveTemplate <= 0)
                    throw new ArgumentException("IdLeaveTemplate is required.");

                var template = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == idLeaveTemplate);

                if (template == null)
                    throw new ArgumentException("Leave template not found.");

                if (template.ApprovlStatus == "APPROVED")
                    throw new ArgumentException("Approved template cannot be resubmitted/rejected.");

                // ✅ Initiate workflow for each record
                var entityCode = _configuration["WorkflowEntityCodes:LeaveTemplate"];

                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    idLeaveTemplate, entityCode, loggedInEmployeeId, approvalStatus, null, null);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error submitting LeaveTemplate for approval. IdLeaveTemplate={Id}",
                    idLeaveTemplate);
                throw;
            }
        }

        #endregion

        public async Task<bool> AddOrUpdateLeaveTemplateDetails(LeaveTemplateDetailsDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ============================
                // 1️⃣ Mandatory validations
                // ============================

                if (dto.IdLeaveTemplate <= 0)
                    throw new ArgumentException("Leave Template is required.");

                if (dto.IdLeaveType <= 0)
                    throw new ArgumentException("Leave Type is required.");

                if (string.IsNullOrWhiteSpace(dto.LeaveTypeName))
                    throw new ArgumentException("Leave Type Name is required.");

                if (string.IsNullOrWhiteSpace(dto.LeaveCode))
                    throw new ArgumentException("Leave Code is required.");

                if (dto.IdYear <= 0)
                    throw new ArgumentException("Year is required.");

                if (dto.EffectiveFrom == DateTime.MinValue || dto.EffectiveTo == DateTime.MinValue)
                    throw new ArgumentException("Effective dates are required.");

                if (dto.EffectiveFrom > dto.EffectiveTo)
                    throw new ArgumentException("EffectiveFrom cannot be greater than EffectiveTo.");

                if (dto.MaxLeavesPerYear <= 0)
                    throw new ArgumentException("MaxLeavesPerYear must be greater than zero.");

                // ============================
                // 2️⃣ Normalize
                // ============================

                dto.LeaveCode = dto.LeaveCode.Trim().ToUpper();
                dto.LeaveTypeName = dto.LeaveTypeName.Trim();
                dto.ApplicableGender = (dto.ApplicableGender ?? "BOTH").ToUpper();

                // ============================
                // 3️⃣ LeaveCode uniqueness
                // ============================

                bool codeExists = await _dbContext.LeaveTemplateDetails.AnyAsync(x =>
                    x.LeaveCode == dto.LeaveCode &&
                    x.IdLeaveTemplate == dto.IdLeaveTemplate &&
                    x.IdYear == dto.IdYear &&
                    x.IdLeaveTemplateDetails != dto.IdLeaveTemplateDetails);

                if (codeExists)
                    throw new ArgumentException($"LeaveCode '{dto.LeaveCode}' already exists.");

                // ============================
                // 4️⃣ Gender validation
                // ============================

                if (!new[] { "MALE", "FEMALE", "BOTH" }.Contains(dto.ApplicableGender))
                    throw new ArgumentException("ApplicableGender must be MALE, FEMALE or BOTH.");

                // ============================
                // 5️⃣ Paid / Unpaid rules
                // ============================

                if (dto.IsPaid && (dto.SalaryDeductionPercent ?? 0) > 0)
                    throw new ArgumentException("Paid leave cannot have salary deduction.");

                if (!dto.IsPaid &&
                    ((dto.SalaryDeductionPercent ?? 0) < 0 ||
                     (dto.SalaryDeductionPercent ?? 0) > 100))
                    throw new ArgumentException("SalaryDeductionPercent must be between 0 and 100.");

                // ============================
                // 6️⃣ Approval rules
                // ============================

                if (dto.RequiresApproval &&
                    (!dto.RequiredApprovalLevel.HasValue || dto.RequiredApprovalLevel <= 0))
                    throw new ArgumentException("Approval level is required.");

                if (!dto.RequiresApproval)
                    dto.RequiredApprovalLevel = 0;

                // ============================
                // 7️⃣ Document rules
                // ============================

                if (dto.RequiresDocument &&
                    (!dto.DocumentRequiredAfterDays.HasValue || dto.DocumentRequiredAfterDays <= 0))
                    throw new ArgumentException("DocumentRequiredAfterDays must be greater than zero.");

                if (!dto.RequiresDocument)
                    dto.DocumentRequiredAfterDays = 0;

                // ============================
                // 8️⃣ Carry forward rules
                // ============================

                if (dto.IsCarryForwardAllowed &&
                    (!dto.MaxCarryForwardDays.HasValue || dto.MaxCarryForwardDays <= 0))
                    throw new ArgumentException("MaxCarryForwardDays must be greater than zero.");

                if (!dto.IsCarryForwardAllowed)
                    dto.MaxCarryForwardDays = 0;

                // ============================
                // 9️⃣ Monthly / Yearly cap
                // ============================

                if (dto.MaxLeavesPerMonth.HasValue &&
                    dto.MaxLeavesPerMonth > dto.MaxLeavesPerYear && dto.MaxLeavesPerMonth > 31)
                    throw new ArgumentException("MaxLeavesPerMonth cannot exceed MaxLeavesPerYear or Number of days in a month.");

                // ============================
                // 🔟 Backdated leave rules
                // ============================

                if (dto.AllowBackdatedLeave &&
                    (!dto.BackdateLimitDays.HasValue || dto.BackdateLimitDays <= 0))
                    throw new ArgumentException("BackdateLimitDays must be greater than zero.");

                if (!dto.AllowBackdatedLeave)
                    dto.BackdateLimitDays = 0;

                // ============================
                // 1️⃣1️⃣ Insert / Update
                // ============================
                int IdLeaveTemplateInsUpdate = 0;
                if (dto.IdLeaveTemplateDetails > 0)
                {
                    // 🔹 Update
                    var entity = await _dbContext.LeaveTemplateDetails
                        .FirstOrDefaultAsync(x => x.IdLeaveTemplateDetails == dto.IdLeaveTemplateDetails);

                    if (entity == null)
                        throw new ArgumentException("LeaveTemplateDetails not found.");
                    IdLeaveTemplateInsUpdate = dto.IdLeaveTemplateDetails;
                    entity.IdLeaveType = dto.IdLeaveType;
                    entity.LeaveTypeName = dto.LeaveTypeName;
                    entity.LeaveCode = dto.LeaveCode;
                    entity.IdYear = dto.IdYear;
                    entity.EffectiveFrom = dto.EffectiveFrom;
                    entity.EffectiveTo = dto.EffectiveTo;
                    entity.ApplicableGender = dto.ApplicableGender;
                    entity.IsPaid = dto.IsPaid;
                    entity.SalaryDeductionPercent = dto.SalaryDeductionPercent;
                    entity.SalaryDeductAfterDays = dto.SalaryDeductAfterDays;
                    entity.AllowHalfDay = dto.AllowHalfDay;
                    entity.RequiresApproval = dto.RequiresApproval;
                    entity.RequiredApprovalLevel = dto.RequiredApprovalLevel;
                    entity.RequiresDocument = dto.RequiresDocument;
                    entity.DocumentRequiredAfterDays = dto.DocumentRequiredAfterDays;
                    entity.IsCarryForwardAllowed = dto.IsCarryForwardAllowed;
                    entity.MaxCarryForwardDays = dto.MaxCarryForwardDays;
                    entity.IncludeHolidaysBetween = dto.IncludeHolidaysBetween;
                    entity.MaxLeavesPerYear = dto.MaxLeavesPerYear;
                    entity.MaxLeavesPerMonth = dto.MaxLeavesPerMonth;
                    entity.AllowBackdatedLeave = dto.AllowBackdatedLeave;
                    entity.BackdateLimitDays = dto.BackdateLimitDays;
                    await _dbContext.SaveChangesAsync();

                }
                else
                {
                    // 🔹 Insert
                    var entity = new LeaveTemplateDetails
                    {
                        IdLeaveTemplate = dto.IdLeaveTemplate,
                        IdLeaveType = dto.IdLeaveType,
                        LeaveTypeName = dto.LeaveTypeName,
                        LeaveCode = dto.LeaveCode,
                        IdYear = dto.IdYear,
                        EffectiveFrom = dto.EffectiveFrom,
                        EffectiveTo = dto.EffectiveTo,
                        ApplicableGender = dto.ApplicableGender,
                        IsPaid = dto.IsPaid,
                        SalaryDeductionPercent = dto.SalaryDeductionPercent,
                        SalaryDeductAfterDays = dto.SalaryDeductAfterDays,
                        AllowHalfDay = dto.AllowHalfDay,
                        RequiresApproval = dto.RequiresApproval,
                        RequiredApprovalLevel = dto.RequiredApprovalLevel,
                        RequiresDocument = dto.RequiresDocument,
                        DocumentRequiredAfterDays = dto.DocumentRequiredAfterDays,
                        IsCarryForwardAllowed = dto.IsCarryForwardAllowed,
                        MaxCarryForwardDays = dto.MaxCarryForwardDays,
                        IncludeHolidaysBetween = dto.IncludeHolidaysBetween,
                        MaxLeavesPerYear = dto.MaxLeavesPerYear,
                        MaxLeavesPerMonth = dto.MaxLeavesPerMonth,
                        AllowBackdatedLeave = dto.AllowBackdatedLeave,
                        BackdateLimitDays = dto.BackdateLimitDays
                    };

                    await _dbContext.LeaveTemplateDetails.AddAsync(entity);
                    await _dbContext.SaveChangesAsync();
                    IdLeaveTemplateInsUpdate = entity.IdLeaveTemplateDetails;

                }

                // ================= SAVE WORKFLOW =================
                List<LeaveWorkFlowDetailDto> wfDto = dto.leaveWorkFlowDetails;
                if (wfDto != null && wfDto.Any())
                {
                    int? idWorkflow = wfDto[0].IdWorkFlowConfig;
                    if (idWorkflow > 0)
                    {
                        var wfConfig = await _dbContext.WorkFlowConfig
                            .FirstOrDefaultAsync(x => x.IdWorkFlowConfig == idWorkflow);

                        if (wfConfig != null)
                        {
                            _dbContext.WorkFlowConfig.Remove(wfConfig);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    //Insert into WorkFlowConfig Table

                    string entityCode = "LEAVE" + "_" + IdLeaveTemplateInsUpdate.ToString();
                    if (dto.IdLeaveTemplateDetails > 0)
                    {
                        var existingWorkflowConfig = await _dbContext.WorkFlowConfig.FirstOrDefaultAsync(x => x.EntityCode == entityCode);

                        if (existingWorkflowConfig != null)
                        {
                            // 🔹 Remove workflow details FIRST
                            var existingDetails = await _dbContext.WorkFlowConfigDetails
                                .Where(x => x.IdWorkFlowConfig == existingWorkflowConfig.IdWorkFlowConfig)
                                .ToListAsync();

                            if (existingDetails.Any())
                                _dbContext.WorkFlowConfigDetails.RemoveRange(existingDetails);

                            // 🔹 Remove workflow header
                            _dbContext.WorkFlowConfig.Remove(existingWorkflowConfig);
                            await _dbContext.SaveChangesAsync();
                        }
                    }

                    var workflowConfig = new WorkFlowConfig
                    {
                        EntityCode = entityCode,
                        EntityName = "Leave Approval Workflow - " + dto.LeaveTypeName,
                        ApprovalCycleCount = wfDto.Count,
                        MainTableName = "LeaveApplications",
                        MainColumnName = "IdEmployee",

                    };

                    _dbContext.WorkFlowConfig.Add(workflowConfig);
                    await _dbContext.SaveChangesAsync();

                    var wfConfig1 = await _dbContext.WorkFlowConfig
                           .FirstOrDefaultAsync(x => x.EntityCode == entityCode);

                    int InsertedIdWorkFlowConfig = 0;
                    if (wfConfig1 != null)
                        InsertedIdWorkFlowConfig = wfConfig1.IdWorkFlowConfig;
                    else
                        return false;

                    // remove existing workflow
                    var existingWorkflow = await _dbContext.WorkFlowConfigDetails
                        .Where(x => x.IdWorkFlowConfig == InsertedIdWorkFlowConfig)
                        .ToListAsync();

                    if (existingWorkflow.Any())
                        _dbContext.WorkFlowConfigDetails.RemoveRange(existingWorkflow);

                    // insert new workflow
                    int levelCount = wfDto.Count;
                    foreach (var wfd in wfDto)
                    {
                        if (wfd.LevelNumber < levelCount)
                            wfd.ApprovalStatusName = "L" + wfd.LevelNumber.ToString() + "_" + wfd.ApprovalStatusName;
                        if (wfd.LevelNumber == levelCount)
                            wfd.ApprovalStatusName = "APPROVED";
                    }
                    var workflowEntities = wfDto.Select(w => new WorkFlowConfigDetails
                    {
                        ApprovalStatusName = w.ApprovalStatusName,
                        ApprovalAuthorityID = w.ApprovalAuthorityID,
                        ApprovalAuthorityType = w.ApprovalAuthorityType,
                        LevelNumber = w.LevelNumber,
                        IdWorkFlowConfig = InsertedIdWorkFlowConfig
                    }).ToList();

                    await _dbContext.WorkFlowConfigDetails.AddRangeAsync(workflowEntities);
                }
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving LeaveTemplateDetails");
                throw;
            }
        }

        public async Task<LeaveTemplateDetailsDto> GetLeaveTemplateDetailById(int idLeaveTemplateDetails)
        {
            try
            {
                if (idLeaveTemplateDetails <= 0)
                    throw new ArgumentException("Invalid LeaveTemplateDetail Id.");

                var detail = await _dbContext.LeaveTemplateDetails
                    .AsNoTracking()
                    .Where(x => x.IdLeaveTemplateDetails == idLeaveTemplateDetails)
                    .Select(x => new LeaveTemplateDetailsDto
                    {
                        IdLeaveTemplateDetails = x.IdLeaveTemplateDetails,
                        IdLeaveTemplate = x.IdLeaveTemplate,
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        LeaveCode = x.LeaveCode,
                        IdYear = x.IdYear,

                        EffectiveFrom = x.EffectiveFrom,
                        EffectiveTo = x.EffectiveTo,

                        ApplicableGender = x.ApplicableGender,
                        IsPaid = x.IsPaid,
                        SalaryDeductionPercent = x.SalaryDeductionPercent,

                        AllowHalfDay = x.AllowHalfDay,
                        RequiresApproval = x.RequiresApproval,
                        RequiredApprovalLevel = x.RequiredApprovalLevel,
                        SalaryDeductAfterDays = x.SalaryDeductAfterDays,
                        RequiresDocument = x.RequiresDocument,
                        DocumentRequiredAfterDays = x.DocumentRequiredAfterDays,

                        IsCarryForwardAllowed = x.IsCarryForwardAllowed,
                        MaxCarryForwardDays = x.MaxCarryForwardDays,

                        IncludeHolidaysBetween = x.IncludeHolidaysBetween,

                        MaxLeavesPerYear = x.MaxLeavesPerYear,
                        MaxLeavesPerMonth = x.MaxLeavesPerMonth,

                        AllowBackdatedLeave = x.AllowBackdatedLeave,
                        BackdateLimitDays = x.BackdateLimitDays
                    })
                    .FirstOrDefaultAsync();

                if (detail == null)
                    throw new ArgumentException("LeaveTemplateDetail not found.");

                detail.leaveWorkFlowDetails = await
                        (
                            from wfd in _dbContext.WorkFlowConfigDetails
                            join wf in _dbContext.WorkFlowConfig
                                on wfd.IdWorkFlowConfig equals wf.IdWorkFlowConfig
                            where wf.EntityCode == "LEAVE_" + detail.IdLeaveTemplateDetails.ToString()
                            select new LeaveWorkFlowDetailDto
                            {
                                IdWorkFlowConfigDetail = wfd.IdWorkFlowConfigDetail,
                                IdWorkFlowConfig = wfd.IdWorkFlowConfig,
                                LevelNumber = wfd.LevelNumber,
                                ApprovalAuthorityType = wfd.ApprovalAuthorityType,
                                ApprovalStatusName = wfd.ApprovalStatusName,
                                ApprovalAuthorityID = wfd.ApprovalAuthorityID
                            }
                        ).ToListAsync();

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching LeaveTemplateDetail. IdLeaveTemplateDetails={IdLeaveTemplateDetails}",
                    idLeaveTemplateDetails);
                throw;
            }
        }

        #region EmployeeLeaveManagement
        public async Task<List<EmployeeLeaveSetupDto>> GetEmployeesLeaveSetup(string? searchText, int? idYear, string? ApprovalStatus)
        {
            try
            {
                searchText = searchText?.Trim();

                var query =
                    from elc in _dbContext.EmployeeLeaveConfigs
                    join emp in _dbContext.Employees
                        on elc.IdEmployee equals emp.IdEmployee
                    join dept in _dbContext.Departments
                        on emp.IdDepartment equals dept.IdDepartment
                    join desig in _dbContext.Designations
                        on emp.IdDesignation equals desig.IdDesignation
                    join lt in _dbContext.LeaveTemplates
                        on elc.IdLeaveTemplate equals lt.IdLeaveTemplate
                    select new
                    {
                        elc,
                        emp,
                        dept,
                        desig,
                        lt
                    };

                // 🔍 SearchText filter (optional)
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(x =>
                        x.emp.FirstName.Contains(searchText) ||
                        x.emp.LastName.Contains(searchText) ||
                        x.dept.DepartmentName.Contains(searchText) ||
                        x.desig.DesignationName.Contains(searchText)
                    );
                }
                if (!string.IsNullOrWhiteSpace(ApprovalStatus))
                {
                    query = query.Where(x => x.elc.ApprovalStatus.ToUpper() == ApprovalStatus.ToUpper());
                }
                // 📅 Year filter (optional)
                if (idYear > 0)
                {
                    query = query.Where(x => x.lt.IdYear == idYear);
                }

                var configs = await query
                    .OrderByDescending(x => x.elc.EffectiveFrom)
                    .Select(x => new EmployeeLeaveSetupDto
                    {
                        // 🔹 Employee Leave Config
                        IdEmployeeLeaveConfig = x.elc.IdEmployeeLeaveConfig,
                        IdEmployee = x.elc.IdEmployee,
                        IdLeaveTemplate = x.elc.IdLeaveTemplate,
                        LeaveTemplateName = x.lt.LeaveTemplateName,
                        IdYear = x.lt.IdYear,

                        EffectiveFrom = x.elc.EffectiveFrom,
                        EffectiveTo = x.elc.EffectiveTo,

                        CreatedBy = x.elc.CreatedBy,
                        CreatedAt = x.elc.CreatedAt,
                        UpdatedBy = x.elc.UpdatedBy,
                        UpdatedAt = x.elc.UpdatedAt,

                        // 🔹 Employee Details
                        EmployeeName = ((x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? "")).Trim(),
                        JoingDate = x.emp.JoiningDate ?? DateTime.MinValue,
                        DepartmentName = x.dept.DepartmentName,
                        DesignationName = x.desig.DesignationName,
                        ApprovalStatus = x.elc.ApprovalStatus,
                        IdApprovedBy = x.elc.IdApprovedBy

                    }).OrderBy(em => em.EmployeeName)
                    .AsNoTracking().ToListAsync();

                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee leave setup. SearchText={SearchText}, IdYear={IdYear}", searchText, idYear);
                throw;
            }
        }


        public async Task<EmployeeLeaveSetupDto?> GetLeaveSetupOfAnEmployee(int idEmployee, int? idYear, DateTime? dateTo)
        {
            try
            {
                // 🔹 Step 1: Resolve Year (ONLY ONE SOURCE)
                int resolvedYear;

                if (idYear.HasValue)
                {
                    resolvedYear = idYear.Value;
                }
                else
                {
                    if (!dateTo.HasValue)
                        throw new ArgumentException("Either IdYear or DateTo must be provided.");

                    var workYear = await _dbContext.WorkYears
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w =>
                            dateTo.Value.Date >= w.WorkDateFrom &&
                            dateTo.Value.Date <= w.WorkDateTo);

                    if (workYear == null)
                    {
                        _logger.LogWarning(
                            "No work year found for DateTo {DateTo}",
                            dateTo.Value);
                        return null;
                    }

                    resolvedYear = workYear.IdWorkYear;
                }

                // 🔹 Step 2: MASTER QUERY
                var config = await
                    (from elc in _dbContext.EmployeeLeaveConfigs
                     join emp in _dbContext.Employees
                         on elc.IdEmployee equals emp.IdEmployee
                     join dept in _dbContext.Departments
                         on emp.IdDepartment equals dept.IdDepartment
                     join desig in _dbContext.Designations
                         on emp.IdDesignation equals desig.IdDesignation
                     join lt in _dbContext.LeaveTemplates
                         on elc.IdLeaveTemplate equals lt.IdLeaveTemplate
                     where elc.IdEmployee == idEmployee
                           && lt.IdYear == resolvedYear
                           && (
                                !dateTo.HasValue ||
                                (
                                    elc.EffectiveFrom.Date <= dateTo.Value.Date &&
                                    (elc.EffectiveTo == null || elc.EffectiveTo.Value.Date >= dateTo.Value.Date)
                                )
                              )
                     orderby elc.EffectiveFrom descending
                     select new EmployeeLeaveSetupDto
                     {
                         IdEmployeeLeaveConfig = elc.IdEmployeeLeaveConfig,
                         IdEmployee = elc.IdEmployee,
                         IdLeaveTemplate = elc.IdLeaveTemplate,
                         LeaveTemplateName = lt.LeaveTemplateName,
                         IdYear = lt.IdYear,

                         EffectiveFrom = elc.EffectiveFrom,
                         EffectiveTo = elc.EffectiveTo,

                         CreatedBy = elc.CreatedBy,
                         CreatedAt = elc.CreatedAt,
                         UpdatedBy = elc.UpdatedBy,
                         UpdatedAt = elc.UpdatedAt,
                         ApprovalStatus = elc.ApprovalStatus,
                         EmployeeName =
                             ((emp.FirstName ?? "") + " " + (emp.LastName ?? "")).Trim(),
                         JoingDate = emp.JoiningDate ?? DateTime.MinValue,
                         DepartmentName = dept.DepartmentName,
                         DesignationName = desig.DesignationName
                     })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (config == null)
                {
                    _logger.LogWarning(
                        "No leave setup found for Employee {EmployeeId}, Year {Year}",
                        idEmployee,
                        resolvedYear);
                    return null;
                }

                // 🔹 Step 3: DETAIL QUERY
                config.Details = await
                    (from elcd in _dbContext.EmployeeLeaveConfigDetails
                     join ltd in _dbContext.LeaveTemplateDetails
                         on elcd.IdLeaveTemplateDetail equals ltd.IdLeaveTemplateDetails
                     where elcd.IdEmployeeLeaveConfig == config.IdEmployeeLeaveConfig
                     select new EmployeeLeaveSetupDetailDto
                     {
                         IdEmployeeLeaveConfigDetail = elcd.IdEmployeeLeaveConfigDetails,
                         IdEmployeeLeaveConfig = elcd.IdEmployeeLeaveConfig,
                         IdLeaveType = elcd.IdLeaveType,
                         IdLeaveTemplateDetail = elcd.IdLeaveTemplateDetail,

                         LeaveTypeName = ltd.LeaveTypeName,
                         LeaveCode = ltd.LeaveCode,

                         AllocatedDaysInYear = elcd.AllocatedDaysInYear,
                         CarryForwardDays = elcd.CarryForwardDays,
                         TotalAllocatedDays = elcd.TotalAllocatedDays,
                         UsedLeaveDays = elcd.UsedLeaveDays,
                         BalanceLeaveDays = elcd.BalanceLeaveDays,
                         SalaryDeductAfterDays = ltd.SalaryDeductAfterDays,
                         ApplicableGender = ltd.ApplicableGender,
                         IsPaid = ltd.IsPaid,
                         SalaryDeductionPercent = ltd.SalaryDeductionPercent,

                         AllowHalfDay = ltd.AllowHalfDay,
                         RequiresApproval = ltd.RequiresApproval,
                         RequiredApprovalLevel = ltd.RequiredApprovalLevel,
                         RequiresDocument = ltd.RequiresDocument,
                         DocumentRequiredAfterDays = ltd.DocumentRequiredAfterDays,
                         IsCarryForwardAllowed = ltd.IsCarryForwardAllowed,
                         MaxCarryForwardDays = ltd.MaxCarryForwardDays,
                         IncludeHolidaysBetween = ltd.IncludeHolidaysBetween,
                         MaxLeavesPerYear = ltd.MaxLeavesPerYear,
                         MaxLeavesPerMonth = ltd.MaxLeavesPerMonth,
                         AllowBackdatedLeave = ltd.AllowBackdatedLeave,
                         BackdateLimitDays = ltd.BackdateLimitDays
                     })
                    .AsNoTracking()
                    .ToListAsync();

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching leave setup for Employee {EmployeeId}",
                    idEmployee);
                throw;
            }
        }

        public async Task<bool> AddUpdateEmployeeLeaveConfig(EmployeeLeaveConfigsPostDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ============================
                // 1️⃣ Basic validations
                // ============================

                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                if (dto.IdLeaveTemplate <= 0)
                    throw new ArgumentException("IdEmployeeLeaveTemplate is required.");

                if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
                    throw new ArgumentException("EffectiveTo cannot be earlier than EffectiveFrom.");

                // ============================
                // 2️⃣ Validate employee exists
                // ============================

                var employee = await _dbContext.Employees
                         .Where(e => e.IdEmployee == dto.IdEmployee)
                         .Select(e => new
                         {
                             gender = e.Gender,
                             idEmployee = e.IdEmployee
                         })
                         .FirstOrDefaultAsync();

                if (employee == null)
                    throw new ArgumentException("Employee not found.");

                // ============================
                // 3️⃣ Validate template exists
                // ============================

                var lvTemplate = await _dbContext.LeaveTemplates.Where(t => t.IdLeaveTemplate == dto.IdLeaveTemplate).FirstOrDefaultAsync();

                if (lvTemplate == null)
                    throw new ArgumentException("Leave template not found.");


                // ============================
                // 4️⃣ Prevent overlapping assignments
                // ============================

                DateTime newFrom = dto.EffectiveFrom.Date;
                DateTime newTo = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

                bool isOverlapping = await _dbContext.EmployeeLeaveConfigs.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee &&
                    x.IdEmployeeLeaveConfig != dto.IdEmployeeLeaveConfig &&
                    x.EffectiveFrom <= newTo &&
                    (x.EffectiveTo == null || x.EffectiveTo >= newFrom));

                if (isOverlapping)
                    throw new ArgumentException("Overlapping leave template assignment exists.");

                EmployeeLeaveConfigs entity;

                // ============================
                // 5️⃣ Insert / Update
                // ============================

                if (dto.IdEmployeeLeaveConfig > 0)
                {
                    entity = await _dbContext.EmployeeLeaveConfigs
                        .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == dto.IdEmployeeLeaveConfig);

                    if (entity == null)
                        throw new ArgumentException("Employee leave config not found.");

                    entity.IdLeaveTemplate = dto.IdLeaveTemplate;
                    entity.EffectiveFrom = dto.EffectiveFrom;
                    entity.EffectiveTo = dto.EffectiveTo;
                    entity.UpdatedAt = DateTime.Now;
                    entity.UpdatedBy = loggedInEmployeeId;
                    entity.ApprovalStatus = "SUBMITTED";
                    await _dbContext.SaveChangesAsync();

                }
                else
                {
                    entity = new EmployeeLeaveConfigs
                    {
                        IdEmployee = dto.IdEmployee,
                        IdLeaveTemplate = dto.IdLeaveTemplate,
                        EffectiveFrom = dto.EffectiveFrom,
                        EffectiveTo = dto.EffectiveTo,
                        CreatedAt = DateTime.Now,
                        ApprovalStatus = "SUBMITTED",
                        CreatedBy = loggedInEmployeeId,                     

                    };
                    await _dbContext.EmployeeLeaveConfigs.AddAsync(entity);
                    await _dbContext.SaveChangesAsync();
                    int configId = entity.IdEmployeeLeaveConfig;
                    //Insert into Employee Leave Config Details from the leave template details.
                    var templateDetails = await _dbContext.LeaveTemplateDetails.Where(x => x.IdLeaveTemplate == dto.IdLeaveTemplate).AsNoTracking().ToListAsync();

                    var validGenders = new[] { employee.gender, "BOTH" };
                    if (templateDetails.Any(td => !validGenders.Contains(td.ApplicableGender)))
                    {
                        throw new ArgumentException(
                            "The selected leave template includes configurations that are not applicable to the employee's gender."
                        );
                    }
                    var detailEntities = templateDetails.Select(td => new EmployeeLeaveConfigDetails
                    {
                        IdEmployeeLeaveConfig = configId,
                        IdYear = lvTemplate.IdYear,
                        IdLeaveTemplateDetail = td.IdLeaveTemplateDetails,
                        IdLeaveType = td.IdLeaveType,
                        AllocatedDaysInYear = td.MaxLeavesPerYear,
                        CarryForwardDays = 0,
                        TotalAllocatedDays = td.MaxLeavesPerYear,
                        UsedLeaveDays = 0,
                        BalanceLeaveDays = td.MaxLeavesPerYear
                    }).ToList();
                    await _dbContext.EmployeeLeaveConfigDetails.AddRangeAsync(detailEntities);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        public async Task<List<LeaveTemplateApplyResult>> ApplyLeaveTemplateToMultipleEmployees(List<int> Idemployees, int IdLeaveTemplate, int IdYear, int loggedInEmployeeId)
        {
            if (Idemployees == null || !Idemployees.Any())
                return null;

            List<LeaveTemplateApplyResult> resultSet = new List<LeaveTemplateApplyResult>();
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Get Template Master
                var template = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == IdLeaveTemplate
                                           && x.IdYear == IdYear);

                if (template == null)
                    throw new Exception("Leave template not found.");

                // 2️⃣ Get Template Details
                var templateDetails = await _dbContext.LeaveTemplateDetails
                    .Where(x => x.IdLeaveTemplate == IdLeaveTemplate
                             && x.IdYear == IdYear)
                    .ToListAsync();

                if (!templateDetails.Any())
                    throw new Exception("Leave template details not found.");

                int insertedCount = 0;

                foreach (var empId in Idemployees)
                {
                    // 3️⃣ Check if already applied
                    var existingConfig = await _dbContext.EmployeeLeaveConfigs
                        .FirstOrDefaultAsync(x => x.IdEmployee == empId && x.IdLeaveTemplate == IdLeaveTemplate);
                    LeaveTemplateApplyResult ltResult = new LeaveTemplateApplyResult();

                    if (existingConfig != null)
                    {
                        ltResult.IdEmployee = empId;
                        ltResult.IsSuccess = false;
                        ltResult.Message = "Configuration already Exists";
                        resultSet.Add(ltResult);
                        continue;
                    }

                    // 4️⃣ Insert EmployeeLeaveConfig
                    var employeeConfig = new EmployeeLeaveConfigs
                    {
                        IdEmployee = empId,
                        IdLeaveTemplate = IdLeaveTemplate,
                        EffectiveFrom = templateDetails.Min(x => x.EffectiveFrom),
                        EffectiveTo = templateDetails.Max(x => x.EffectiveTo),
                        CreatedBy = loggedInEmployeeId, // replace with logged user
                        CreatedAt = DateTime.Now,
                        ApprovalStatus = "SUBMITTED",
                    };

                    await _dbContext.EmployeeLeaveConfigs.AddAsync(employeeConfig);
                    await _dbContext.SaveChangesAsync(); // get IdEmployeeLeaveConfig

                    // 5️⃣ Insert EmployeeLeaveConfigDetails
                    foreach (var detail in templateDetails)
                    {
                        var allocatedDays = detail.MaxLeavesPerYear;

                        var configDetail = new EmployeeLeaveConfigDetails
                        {
                            IdEmployeeLeaveConfig = employeeConfig.IdEmployeeLeaveConfig,
                            IdLeaveTemplateDetail = detail.IdLeaveTemplateDetails,
                            IdLeaveType = detail.IdLeaveType,
                            AllocatedDaysInYear = allocatedDays,
                            CarryForwardDays = 0,
                            TotalAllocatedDays = allocatedDays,
                            UsedLeaveDays = 0,
                            BalanceLeaveDays = allocatedDays
                        };

                        await _dbContext.EmployeeLeaveConfigDetails.AddAsync(configDetail);
                    }
                    ltResult.IdEmployee = empId;
                    ltResult.IsSuccess = true;
                    ltResult.Message = "Successfully Configured ";
                    resultSet.Add(ltResult);
                    insertedCount++;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return resultSet;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<int> AddUpdateEmployeeLeaveConfigWithDetails(EmployeeLeaveConfigWithDetailsPostDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ============================
                // 1️⃣ Basic Validations
                // ============================

                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("Employee is required.");

                if (dto.IdLeaveTemplate <= 0)
                    throw new ArgumentException("Leave template is required.");

                if (dto.EffectiveTo.HasValue &&
                    dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
                    throw new ArgumentException("EffectiveTo cannot be earlier than EffectiveFrom.");

                var employee = await _dbContext.Employees
                    .Where(e => e.IdEmployee == dto.IdEmployee)
                    .Select(e => new { e.IdEmployee, e.Gender })
                    .FirstOrDefaultAsync();

                if (employee == null)
                    throw new ArgumentException("Employee not found.");

                // ============================
                // 2️⃣ Prevent overlapping master
                // ============================

                DateTime newFrom = dto.EffectiveFrom.Date;
                DateTime newTo = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

                bool overlapping = await _dbContext.EmployeeLeaveConfigs.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee &&
                    x.IdEmployeeLeaveConfig != dto.IdEmployeeLeaveConfig &&
                    x.EffectiveFrom <= newTo &&
                    (x.EffectiveTo == null || x.EffectiveTo >= newFrom));

                if (overlapping)
                    throw new ArgumentException("Overlapping leave template assignment exists.");

                EmployeeLeaveConfigs master;

                // ============================
                // 3️⃣ Insert / Update Master
                // ============================

                if (dto.IdEmployeeLeaveConfig > 0)
                {
                    master = await _dbContext.EmployeeLeaveConfigs.FirstOrDefaultAsync(x =>
                            x.IdEmployeeLeaveConfig == dto.IdEmployeeLeaveConfig);

                    if (master == null)
                        throw new ArgumentException("Employee leave config not found.");

                    master.IdLeaveTemplate = dto.IdLeaveTemplate;
                    master.EffectiveFrom = dto.EffectiveFrom;
                    master.EffectiveTo = dto.EffectiveTo;
                    master.UpdatedAt = DateTime.Now;
                    master.UpdatedBy = loggedInEmployeeId;
                }
                else
                {
                    master = new EmployeeLeaveConfigs
                    {
                        IdEmployee = dto.IdEmployee,
                        IdLeaveTemplate = dto.IdLeaveTemplate,
                        EffectiveFrom = dto.EffectiveFrom,
                        EffectiveTo = dto.EffectiveTo,
                        CreatedAt = DateTime.Now,
                        CreatedBy = loggedInEmployeeId,
                        ApprovalStatus = "SUBMITTED"
                    };

                    await _dbContext.EmployeeLeaveConfigs.AddAsync(master);
                    await _dbContext.SaveChangesAsync();
                }

                int masterId = master.IdEmployeeLeaveConfig;

                // ============================
                // 4️⃣ Process Details
                // ============================

                foreach (var detailDto in dto.Details)
                {
                    if (detailDto.IdLeaveType <= 0)
                        throw new ArgumentException("LeaveType is required.");

                    // Prevent duplicate leave types inside same request
                    if (dto.Details.Count(d => d.IdLeaveType == detailDto.IdLeaveType) > 1)
                        throw new ArgumentException("Duplicate leave types in request.");

                    if (detailDto.IdEmployeeLeaveConfigDetails > 0)
                    {
                        // UPDATE
                        var entity = await _dbContext.EmployeeLeaveConfigDetails
                            .FirstOrDefaultAsync(x =>
                                x.IdEmployeeLeaveConfigDetails ==
                                detailDto.IdEmployeeLeaveConfigDetails);

                        if (entity == null)
                            throw new ArgumentException("Leave config detail not found.");

                        decimal totalAllocated =
                            detailDto.AllocatedDaysInYear + entity.CarryForwardDays;

                        if (entity.UsedLeaveDays > totalAllocated)
                            throw new ArgumentException(
                                "Used leave exceeds total allocation.");

                        entity.AllocatedDaysInYear = detailDto.AllocatedDaysInYear;
                        entity.TotalAllocatedDays = totalAllocated;
                        entity.BalanceLeaveDays =
                            totalAllocated - entity.UsedLeaveDays;
                    }
                    else
                    {
                        // INSERT
                        var entity = new EmployeeLeaveConfigDetails
                        {
                            IdEmployeeLeaveConfig = masterId,
                            IdLeaveTemplateDetail = detailDto.IdLeaveTemplateDetail,
                            IdLeaveType = detailDto.IdLeaveType,
                            AllocatedDaysInYear = detailDto.AllocatedDaysInYear,
                            CarryForwardDays = 0,
                            UsedLeaveDays = 0,
                            TotalAllocatedDays = detailDto.AllocatedDaysInYear,
                            BalanceLeaveDays = detailDto.AllocatedDaysInYear
                        };

                        await _dbContext.EmployeeLeaveConfigDetails.AddAsync(entity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return masterId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving Employee Leave Config with details.");
                throw;
            }
        }

        public async Task<bool> AddUpdateEmployeeLeaveConfigDetails(EmployeeLeaveConfigDetailsPostDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ============================
                // 1️⃣ Mandatory validations
                // ============================

                if (dto.IdEmployeeLeaveConfig <= 0)
                    throw new ArgumentException("EmployeeLeaveConfig is required.");

                if (dto.IdLeaveTemplateDetail <= 0)
                    throw new ArgumentException("LeaveTemplateDetail is required.");

                if (dto.IdLeaveType <= 0)
                    throw new ArgumentException("LeaveType is required.");

                if (dto.AllocatedDaysInYear < 0)
                    throw new ArgumentException("AllocatedDaysInYear cannot be negative.");

                // ============================
                // 2️⃣ Validate master exists
                // ============================

                bool configExists = await _dbContext.EmployeeLeaveConfigs
                    .AnyAsync(x => x.IdEmployeeLeaveConfig == dto.IdEmployeeLeaveConfig);

                if (!configExists)
                    throw new ArgumentException("EmployeeLeaveConfig not found.");

                // ============================
                // 3️⃣ Prevent duplicate LeaveType
                // ============================

                bool duplicateLeaveType = await _dbContext.EmployeeLeaveConfigDetails.AnyAsync(x =>
                    x.IdEmployeeLeaveConfig == dto.IdEmployeeLeaveConfig &&
                    x.IdLeaveType == dto.IdLeaveType &&
                    x.IdEmployeeLeaveConfigDetails != dto.IdEmployeeLeaveConfigDetails);

                if (duplicateLeaveType)
                    throw new ArgumentException("LeaveType already exists for this employee.");

                // ============================
                // 5️⃣ Insert / Update
                // ============================
                int idSubmitted = 0;
                if (dto.IdEmployeeLeaveConfigDetails > 0)
                {
                    // 🔹 Update (preserve CF & Used)
                    var entity = await _dbContext.EmployeeLeaveConfigDetails
                        .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfigDetails == dto.IdEmployeeLeaveConfigDetails);

                    if (entity == null)
                        throw new ArgumentException("EmployeeLeaveConfigDetails not found.");

                    // 🔒 Preserve existing values
                    decimal existingCarryForward = entity.CarryForwardDays;
                    decimal existingUsedDays = entity.UsedLeaveDays;

                    // 🔄 Recalculate totals using NEW allocation + EXISTING usage
                    decimal totalAllocated = dto.AllocatedDaysInYear + existingCarryForward;

                    if (existingUsedDays > totalAllocated)
                        throw new ArgumentException(
                            "Used leave days exceed total allocated days after update.");

                    decimal balance = totalAllocated - existingUsedDays;

                    // ✅ Update only allowed fields
                    entity.AllocatedDaysInYear = dto.AllocatedDaysInYear;
                    entity.TotalAllocatedDays = totalAllocated;
                    entity.BalanceLeaveDays = balance;
                    await _dbContext.SaveChangesAsync();
                    idSubmitted = dto.IdEmployeeLeaveConfig;
                }

                else
                {
                    // 🔹 Insert
                    var entity = new EmployeeLeaveConfigDetails
                    {
                        IdEmployeeLeaveConfig = dto.IdEmployeeLeaveConfig,
                        IdLeaveTemplateDetail = dto.IdLeaveTemplateDetail,
                        IdLeaveType = dto.IdLeaveType,
                        AllocatedDaysInYear = dto.AllocatedDaysInYear,
                        TotalAllocatedDays = dto.AllocatedDaysInYear,
                        CarryForwardDays = 0,
                        UsedLeaveDays = 0
                    };

                    await _dbContext.EmployeeLeaveConfigDetails.AddAsync(entity);
                    await _dbContext.SaveChangesAsync();
                    idSubmitted = entity.IdEmployeeLeaveConfig;

                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error saving EmployeeLeaveConfigDetails. IdEmployeeLeaveConfig={IdEmployeeLeaveConfig}",
                    dto.IdEmployeeLeaveConfig);
                throw;
            }
        }

        public async Task<bool> SubmitEmployeeLeaveConfigForApproval(int IdEmployeeLeaveConfig, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (IdEmployeeLeaveConfig <= 0)
                    throw new ArgumentException("IdLeaveTemplate is required.");

                var template = await _dbContext.EmployeeLeaveConfigs
                    .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == IdEmployeeLeaveConfig);

                if (template == null)
                    throw new ArgumentException("Leave Config not found.");

                if (template.ApprovalStatus == "APPROVED")
                    throw new ArgumentException("Approved template cannot be resubmitted/rejected.");

                // ✅ Update approval status
                template.ApprovalStatus = "SUBMITTED";
                template.UpdatedBy = loggedInEmployeeId;
                template.UpdatedAt = DateTime.Now;

                _dbContext.EmployeeLeaveConfigs.Update(template);
                await _dbContext.SaveChangesAsync();

                // ✅ Initiate workflow for each record
                var entityCode = _configuration["WorkflowEntityCodes:EmployeeLeaveConfig"];

                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    IdEmployeeLeaveConfig, entityCode, loggedInEmployeeId, "SUBMITTED", null, null);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error submitting LeaveTemplate for approval. IdLeaveTemplate={Id}",
                    IdEmployeeLeaveConfig);
                throw;
            }
        }

        public async Task<bool> ApproveEmployeeLeaveConfig(int IdEmployeeLeaveConfig, string approvalStatus, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (IdEmployeeLeaveConfig <= 0)
                    throw new ArgumentException("IdLeaveTemplate is required.");

                var template = await _dbContext.EmployeeLeaveConfigs
                    .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == IdEmployeeLeaveConfig);

                if (template == null)
                    throw new ArgumentException("Leave template not found.");

                if (template.ApprovalStatus == "APPROVED")
                    throw new ArgumentException("Approved template cannot be resubmitted/rejected.");

                // ✅ Initiate workflow for each record
                var entityCode = _configuration["WorkflowEntityCodes:EmployeeLeaveConfig"];

                var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                    IdEmployeeLeaveConfig, entityCode, loggedInEmployeeId, approvalStatus, null, null);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error submitting LeaveTemplate for approval. IdLeaveTemplate={Id}",
                    IdEmployeeLeaveConfig);
                throw;
            }
        }


        public async Task<bool> ApproveEmployeeLeaveConfigMultiple(List<int> IdEmployeeLeaveConfigs, string approvalStatus, int loggedInEmployeeId,
                            string? reason)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (IdEmployeeLeaveConfigs == null || !IdEmployeeLeaveConfigs.Any())
                    throw new ArgumentException("At least one EmployeeLeaveConfig Id is required.");

                if (approvalStatus == "REJECTED" && string.IsNullOrWhiteSpace(reason))
                    throw new ArgumentException("Reason is required when rejecting.");

                var entityCode = _configuration["WorkflowEntityCodes:EmployeeLeaveConfig"];

                var templates = await _dbContext.EmployeeLeaveConfigs
                    .Where(x => IdEmployeeLeaveConfigs.Contains(x.IdEmployeeLeaveConfig))
                    .ToListAsync();

                if (!templates.Any())
                    throw new ArgumentException("No leave config records found.");

                foreach (var template in templates)
                {
                    if (template.ApprovalStatus == "APPROVED")
                        throw new ArgumentException(
                            $"Leave config {template.IdEmployeeLeaveConfig} is already approved.");

                    await _approvalWorkflowService.InitiateApprovalWorkflow(
                        template.IdEmployeeLeaveConfig,
                        entityCode,
                        loggedInEmployeeId,
                        approvalStatus,
                        0,
                        reason);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "Error processing multiple EmployeeLeaveConfig approvals.");

                throw;
            }
        }

        #endregion

        #region LeaveApplications
        public async Task<PagedResultDto<LeaveApplicationListDto>> GetLeaveApplications(
                int loggedInEmployeeId,
                int? idEmployee,
                string? approvalStatus,
                string? applicationStatus,
                DateTime? fromDate,
                DateTime? toDate,
                int? idLeaveType,
                string? SearchText,
                PagingRequestDto paging)
        {
            try
            {
                if (paging.PageNumber <= 0) paging.PageNumber = 1;
                if (paging.PageSize <= 0) paging.PageSize = 10;

                // ✅ Permission logic:
                // Default employee can only see own leave applications.
                // If Manager/HR permission exists → allow seeing others.

                bool canViewAll = false;

                // Example permission:
                //var screenCode = _configuration["ScreenCodes:LeaveApplications"];
                // canViewAll = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "V");

                // ✅ if no permission → force employee scope
                if (!canViewAll)
                {
                    idEmployee = loggedInEmployeeId;
                }

                var query = from la in _dbContext.LeaveApplications
                            join emp in _dbContext.Employees on la.IdEmployee equals emp.IdEmployee
                            join dept in _dbContext.Departments on emp.IdDepartment equals dept.IdDepartment
                            join desig in _dbContext.Designations on emp.IdDesignation equals desig.IdDesignation
                            select new
                            {
                                la,
                                emp,
                                dept,
                                desig
                            };

                if (idEmployee.HasValue && idEmployee.Value > 0)
                    query = query.Where(x => x.la.IdEmployee == idEmployee.Value);

                if (idLeaveType.HasValue && idLeaveType.Value > 0)
                    query = query.Where(x => x.la.IdLeaveType == idLeaveType.Value);

                if (!string.IsNullOrWhiteSpace(approvalStatus))
                    query = query.Where(x => x.la.ApprovalStatus == approvalStatus.Trim());

                if (!string.IsNullOrWhiteSpace(applicationStatus))
                    query = query.Where(x => x.la.ApplicationStatus == applicationStatus.Trim());

                query = query.Where(x =>
                   (x.emp.FirstName ?? "").ToUpper().Contains(SearchText) ||
                   (x.emp.LastName ?? "").ToUpper().Contains(SearchText) ||
                   (x.desig.DesignationName ?? "").ToUpper().Contains(SearchText) ||
                   (x.dept.DepartmentName ?? "").ToUpper().Contains(SearchText)
                    );

                if (fromDate.HasValue)
                    query = query.Where(x => x.la.FromDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.la.ToDate.Date <= toDate.Value.Date);

                int totalRecords = await query.CountAsync();

                // ✅ Sorting
                bool isDesc = paging.SortOrder?.ToUpper() != "ASC";

                query = paging.SortBy?.ToUpper() switch
                {
                    "FROMDATE" => isDesc ? query.OrderByDescending(x => x.la.FromDate) : query.OrderBy(x => x.la.FromDate),
                    "TODATE" => isDesc ? query.OrderByDescending(x => x.la.ToDate) : query.OrderBy(x => x.la.ToDate),
                    _ => isDesc ? query.OrderByDescending(x => x.la.AppliedOn) : query.OrderBy(x => x.la.AppliedOn)
                };

                // ✅ Paging
                int skip = (paging.PageNumber - 1) * paging.PageSize;

                var data = await query
                    .Skip(skip)
                    .Take(paging.PageSize)
                    .Select(x => new LeaveApplicationListDto
                    {
                        IdLeaveApplication = x.la.IdLeaveApplication,
                        IdEmployee = x.la.IdEmployee,
                        IdLeaveType = x.la.IdLeaveType,
                        LeaveTypeName = x.la.LeaveTypeName,
                        FromDate = x.la.FromDate,
                        ToDate = x.la.ToDate,
                        TotalLeaveDays = x.la.TotalLeaveDays,
                        ApprovalStatus = x.la.ApprovalStatus,
                        ApplicationStatus = x.la.ApplicationStatus,
                        AppliedOn = x.la.AppliedOn,
                        EmployeeName = x.emp.FirstName + " " + x.emp.LastName,
                        DesignationName = x.desig.DesignationName,
                        DepartmentName = x.dept.DepartmentName
                    })
                    .ToListAsync();

                return new PagedResultDto<LeaveApplicationListDto>
                {
                    TotalRecords = totalRecords,
                    PageNumber = paging.PageNumber,
                    PageSize = paging.PageSize,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Applications.");
                throw;
            }
        }

        public async Task<List<int>> GetLeaveTemplateApprovers()
        {
            try
            {
                // 🔹 Get all workflow config details for LEAVETEMPLATE
                var workflowDetails = await (
                    from wf in _dbContext.WorkFlowConfig
                    join wfd in _dbContext.WorkFlowConfigDetails
                        on wf.IdWorkFlowConfig equals wfd.IdWorkFlowConfig
                    where wf.EntityCode == "LEAVETEMPLATE"
                    select new
                    {
                        wfd.ApprovalAuthorityType,
                        wfd.ApprovalAuthorityID
                    }).ToListAsync();

                var approverEmployeeIds = new List<int>();

                foreach (var w in workflowDetails)
                {
                    if (w.ApprovalAuthorityType == "ROLE" && w.ApprovalAuthorityID != null)
                    {
                        int designationId = w.ApprovalAuthorityID.Value;

                        var employees = await _dbContext.Employees
                           .Where(em => em.IdDesignation == designationId && em.IdEmployee.HasValue)
                           .Select(em => em.IdEmployee.Value)
                           .ToListAsync();

                        approverEmployeeIds.AddRange(employees);
                    }
                }

                return approverEmployeeIds.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Template approvers.");
                throw;
            }
        }

        public async Task<List<int>> GetEmployeeLeaveConfigApprovers()
        {
            try
            {
                // 🔹 Get all workflow config details for LEAVETEMPLATE
                var workflowDetails = await (
                    from wf in _dbContext.WorkFlowConfig
                    join wfd in _dbContext.WorkFlowConfigDetails
                        on wf.IdWorkFlowConfig equals wfd.IdWorkFlowConfig
                    where wf.EntityCode == "EMPLEAVECONFIG"
                    select new
                    {
                        wfd.ApprovalAuthorityType,
                        wfd.ApprovalAuthorityID
                    }).ToListAsync();

                var approverEmployeeIds = new List<int>();

                foreach (var w in workflowDetails)
                {
                    if (w.ApprovalAuthorityType == "ROLE" && w.ApprovalAuthorityID != null)
                    {
                        int designationId = w.ApprovalAuthorityID.Value;

                        var employees = await _dbContext.Employees
                           .Where(em => em.IdDesignation == designationId && em.IdEmployee.HasValue)
                           .Select(em => em.IdEmployee.Value)
                           .ToListAsync();
                        approverEmployeeIds.AddRange(employees);
                    }
                }
                return approverEmployeeIds.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Template approvers.");
                throw;
            }
        }

        public async Task<IEnumerable<LeaveApplicationListDto>> GetLeaveApplicationsForApproval_old(
        int loggedInEmployeeId, string? approvalStatus, string? SearchText, DateTime? fromDate)
        {
            try
            {

                var query = from la in _dbContext.LeaveApplications
                            join emp in _dbContext.Employees on la.IdEmployee equals emp.IdEmployee
                            join dept in _dbContext.Departments on emp.IdDepartment equals dept.IdDepartment
                            join desig in _dbContext.Designations on emp.IdDesignation equals desig.IdDesignation
                            select new
                            {
                                la,
                                emp,
                                dept,
                                desig
                            };

                /*
                int? idHRDept = _dbContext.Departments.Where(d => d.DepartmentCode == "HRD").FirstOrDefault().IdDepartment;

                bool isHRLoginned = false;
                if(idHRDept > 0)
                {
                    int? idLoginnedHRDept = _dbContext.Employees.Where(d => d.IdDepartment == idHRDept).FirstOrDefault().IdDepartment;
                    if (idHRDept == idLoginnedHRDept)
                        isHRLoginned = true;
                }
                if (!isHRLoginned) 
                {
                    query = query.Where(x => x.emp.ReportingTo == loggedInEmployeeId);
                }
                */

                var approveDetails = _dbContext.ApprovalWorkFlowAllocations
                    .Where(a =>
                        a.EntityCode.Contains("LEAVE") &&
                        ("," + a.TargetIdEmployee + ",").Contains("," + loggedInEmployeeId.ToString() + ","));

                if (!string.IsNullOrWhiteSpace(approvalStatus))
                    query = query.Where(x => x.la.ApprovalStatus == approvalStatus.Trim());

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(x =>
                       (x.emp.FirstName ?? "").ToUpper().Contains(SearchText) ||
                       (x.emp.LastName ?? "").ToUpper().Contains(SearchText) ||
                       (x.desig.DesignationName ?? "").ToUpper().Contains(SearchText) ||
                       (x.dept.DepartmentName ?? "").ToUpper().Contains(SearchText)
                        );
                }
                if (fromDate.HasValue)
                    query = query.Where(x => x.la.FromDate.Date >= fromDate.Value.Date);

                int totalRecords = await query.CountAsync();

                // ✅ Paging

                var data = await query
                    .Select(x => new LeaveApplicationListDto
                    {
                        IdLeaveApplication = x.la.IdLeaveApplication,
                        EmployeeCode = x.emp.EmployeeCode,
                        IdEmployee = x.la.IdEmployee,
                        IdLeaveTemplateDetail = x.la.IdLeaveTemplateDetail,
                        IdLeaveType = x.la.IdLeaveType,
                        LeaveTypeName = x.la.LeaveTypeName,
                        FromDate = x.la.FromDate,
                        ToDate = x.la.ToDate,
                        TotalLeaveDays = x.la.TotalLeaveDays,
                        ApprovalStatus = x.la.ApprovalStatus,
                        ApplicationStatus = String.IsNullOrEmpty(x.la.ApplicationStatus) ? "" : x.la.ApplicationStatus,
                        AppliedOn = x.la.AppliedOn,
                        EmployeeName = ((x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? "")).Trim(),
                        DesignationName = x.desig.DesignationName,
                        DepartmentName = x.dept.DepartmentName,
                        Reason = x.la.Reason

                    })
                    .ToListAsync();
                return data ?? new List<LeaveApplicationListDto>();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Applications.");
                throw;
            }
        }

        public async Task<IEnumerable<LeaveApplicationListDto>> GetLeaveApplicationsForApproval(
            int loggedInEmployeeId, string? approvalStatus, string? searchText, DateTime? fromDate)
        {
            try
            {
                var query =
                    from la in _dbContext.LeaveApplications
                    join emp in _dbContext.Employees
                        on la.IdEmployee equals emp.IdEmployee
                    join dept in _dbContext.Departments
                        on emp.IdDepartment equals dept.IdDepartment
                    join desig in _dbContext.Designations
                        on emp.IdDesignation equals desig.IdDesignation
                    join awa in _dbContext.ApprovalWorkFlowAllocations
                        on la.IdLeaveApplication equals awa.EntityTablePrimaryKeyID
                    where awa.EntityCode.StartsWith("LEAVE_")
                          && ("," + awa.TargetIdEmployee + ",")
                                .Contains("," + loggedInEmployeeId + ",")
                    select new
                    {
                        la,
                        emp,
                        dept,
                        desig,
                        awa
                    };

                // 🔹 Optional filters
                if (!string.IsNullOrWhiteSpace(approvalStatus))
                {
                    if (approvalStatus.ToUpper() == "SUBMITTED")
                        query = query.Where(x => x.awa.ActionStatus == null &&
                            (x.la.ApprovalStatus != "CANCELLED" && x.la.ApprovalStatus != "REJECTED"));
                    if (approvalStatus.ToUpper() == "APPROVED")
                        query = query.Where(x => x.awa.ActionStatus.Contains("APPROVE") &&
                         (x.la.ApprovalStatus != "CANCELLED" && x.la.ApprovalStatus != "REJECTED"));
                    if (approvalStatus.ToUpper() == "REJECTED")
                        query = query.Where(x => x.la.ApprovalStatus.Contains("REJECTED"));
                    if (approvalStatus.ToUpper() == "CANCELLED")
                        query = query.Where(x => x.la.ApprovalStatus.Contains("CANCELLED"));
                }
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    searchText = searchText.ToUpper();

                    query = query.Where(x =>
                        (x.emp.FirstName ?? "").ToUpper().Contains(searchText) ||
                        (x.emp.LastName ?? "").ToUpper().Contains(searchText) ||
                        (x.desig.DesignationName ?? "").ToUpper().Contains(searchText) ||
                        (x.dept.DepartmentName ?? "").ToUpper().Contains(searchText));
                }

                if (fromDate.HasValue)
                    query = query.Where(x => x.la.FromDate >= fromDate.Value.Date);

                var data = await query
                          .AsNoTracking()
                          .Select(x => new LeaveApplicationListDto
                          {
                              IdLeaveApplication = x.la.IdLeaveApplication,
                              IdEmployee = x.la.IdEmployee,
                              EmployeeCode = x.emp.EmployeeCode,
                              IdLeaveTemplateDetail = x.la.IdLeaveTemplateDetail,
                              IdLeaveType = x.la.IdLeaveType,
                              LeaveTypeName = x.la.LeaveTypeName,
                              FromDate = x.la.FromDate,
                              ToDate = x.la.ToDate,
                              TotalLeaveDays = x.la.TotalLeaveDays,
                              ApprovalStatus = x.la.ApprovalStatus,
                              ApplicationStatus = x.la.ApplicationStatus ?? "",
                              AppliedOn = x.la.AppliedOn,
                              EmployeeName = ((x.emp.FirstName ?? "") + " " + (x.emp.LastName ?? "")).Trim(),
                              DesignationName = x.desig.DesignationName,
                              DepartmentName = x.dept.DepartmentName,
                              Reason = x.la.Reason,
                              ActionStatusByUser = x.awa.ActionStatus,
                              ActionStatusDateByUser = x.awa.ActionDate,

                              // ✅ EXISTS check (translated to SQL EXISTS)
                              HasDocuments = _dbContext.LeaveApplicationDocuments
                                  .Any(d => d.IdLeaveApplication == x.la.IdLeaveApplication)
                          })
                          .ToListAsync();

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Applications for approval.");
                throw;
            }
        }

        public async Task<string> GetApprovalLevelDetails(int idLeaveTemplateDetail, int idEmployee, int loggedInEmployeeId)
        {
            // 🔹 Fetch workflow configuration
            var workflowDetails = await
                (from wc in _dbContext.WorkFlowConfig
                 join wcd in _dbContext.WorkFlowConfigDetails
                     on wc.IdWorkFlowConfig equals wcd.IdWorkFlowConfig
                 where wc.EntityCode == "LEAVE_" + idLeaveTemplateDetail
                 orderby wcd.LevelNumber
                 select new
                 {
                     wcd.LevelNumber,
                     wcd.ApprovalAuthorityID,
                     wcd.ApprovalAuthorityType
                 })
                .AsNoTracking()
                .ToListAsync();

            if (!workflowDetails.Any())
                return "[]";

            // 🔹 Fetch employee once (for reporting officer)
            var employee = await _dbContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);

            var approvers = new List<LeaveApprovalDetailsDto>();

            foreach (var wf in workflowDetails)
            {
                // 🔹 Reporting Officer
                if (wf.ApprovalAuthorityType == "REPOFFICER")
                {
                    if (employee?.ReportingTo != null)
                    {
                        var reportingOfficer = await _dbContext.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e => e.IdEmployee == employee.ReportingTo);

                        if (reportingOfficer != null)
                        {
                            approvers.Add(new LeaveApprovalDetailsDto
                            {
                                level = wf.LevelNumber,
                                type = "REPOFFICER",
                                id = reportingOfficer.IdEmployee.Value,
                                name = $"{reportingOfficer.FirstName} {reportingOfficer.LastName}".Trim(),
                                status = "PENDING",
                                statusDate = null
                            });
                        }
                    }
                }
                // 🔹 Role / Department based
                else if (wf.ApprovalAuthorityType == "ROLE")
                {
                    var desig = await _dbContext.Designations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.IdDesignation == wf.ApprovalAuthorityID);

                    if (desig != null)
                    {
                        approvers.Add(new LeaveApprovalDetailsDto
                        {
                            level = wf.LevelNumber,
                            type = "ROLE",
                            id = desig.IdDesignation,
                            name = desig.DesignationName,
                            status = "PENDING",
                            statusDate = null
                        });
                    }
                }
            }

            // 🔹 Serialize ONLY list of ApproverDto
            string approverJson = JsonSerializer.Serialize(
                approvers,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            return approverJson;
        }



        /*
        public async Task<string> GetApprovalLevelDetails(int idLeaveApplication,int idLeaveTemplateDetail,int idEmployee)
        {
            // 🔹 Fetch workflow config details
            var workflowDetails = await
                (from wc in _dbContext.WorkFlowConfig
                 join wcd in _dbContext.WorkFlowConfigDetails
                     on wc.IdWorkFlowConfig equals wcd.IdWorkFlowConfig
                 where wc.EntityCode == "LEAVE_" + idLeaveTemplateDetail
                 select new
                 {
                     wcd.ApprovalAuthorityID,
                     wcd.ApprovalStatusName,
                     wcd.ApprovalAuthorityType
                 })
                .AsNoTracking()
                .ToListAsync();

            if (!workflowDetails.Any())
                return string.Empty;

            var approverNames = new List<string>();

            // 🔹 Fetch employee once
            var employee = await _dbContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);

            foreach (var wcdet in workflowDetails)
            {
                // 🔹 Reporting Officer
                if (wcdet.ApprovalAuthorityType == "REPOFFICER")
                {
                    if (employee?.ReportingTo != null)
                    {
                        var reportingOfficer = await _dbContext.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e => e.IdEmployee == employee.ReportingTo);

                        if (reportingOfficer != null)
                        {
                            approverNames.Add(
                                $"{reportingOfficer.FirstName} {reportingOfficer.LastName}".Trim());
                        }
                    }
                }

                // 🔹 Role / Department based approval
                else if (wcdet.ApprovalAuthorityType == "ROLE")
                {
                    var department = await _dbContext.Departments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.IdDepartment == wcdet.ApprovalAuthorityID);

                    if (department != null)
                    {
                        approverNames.Add(department.DepartmentName);
                    }
                }
            }

            // 🔹 Final formatted string
            return string.Join(" | ", approverNames);
        }
        */

        /*
        public async Task<IEnumerable<LeaveApplicationListDto>> GetLeaveApplicationsForApproval111(int loggedInEmployeeId, string? approvalStatus, string? SearchText, DateTime? fromDate)
        {
            try
            {


                var data = await _dbContext.LeaveApplicationListDtos
           .FromSqlRaw(
               "EXEC sp_GetLeaveApplicationsForApproval @LoggedInEmployeeId, @ApprovalStatus, @SearchText, @FromDate",
               new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId),
               new SqlParameter("@ApprovalStatus", (object?)approvalStatus ?? DBNull.Value),
               new SqlParameter("@SearchText", (object?)searchText ?? DBNull.Value),
               new SqlParameter("@FromDate", (object?)fromDate ?? DBNull.Value)
           )
           .AsNoTracking()
           .ToListAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Leave Applications.");
                throw;
            }
        }

        */

        public async Task<bool> SubmitLeaveApplicationApproval(List<int> idChanges, string approvalStatus, string? remarks, int loggedInEmployeeId)
        {
            try
            {
                var leaveApplications = await _dbContext.LeaveApplications
                    .Where(x => idChanges.Contains(x.IdLeaveApplication))
                    .ToListAsync();

                if (!leaveApplications.Any())
                    return false;
                if (remarks == null)
                    remarks = "";
                foreach (var la in leaveApplications)
                {
                    la.ApprovedBy = loggedInEmployeeId;
                    var result = await _approvalWorkflowService.InitiateApprovalWorkflow(
                        la.IdLeaveApplication,
                        "LEAVE_" + la.IdLeaveTemplateDetail,
                        loggedInEmployeeId,
                        approvalStatus, null,
                        remarks,
                        1
                    );

                    if (approvalStatus == "REJECTED")
                    {
                        var empLeaveConfig = await _dbContext.EmployeeLeaveConfigs
                        .FirstOrDefaultAsync(x => x.IdEmployee == la.IdEmployee &&
                        la.FromDate >= x.EffectiveFrom && (x.EffectiveTo == null || la.FromDate <= x.EffectiveTo));
                        //Update Used and Balance details in employee leave config detail
                        var empLeaveConfigDetailEntity = await _dbContext.EmployeeLeaveConfigDetails
                            .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == empLeaveConfig.IdEmployeeLeaveConfig && x.IdLeaveType == la.IdLeaveType);

                        if (empLeaveConfigDetailEntity == null)
                            throw new InvalidOperationException("Employee leave configuration detail not found.");
                        var usedLeaveDays = await _dbContext.LeaveApplications
                                  .Where(x =>
                                  x.IdEmployee == la.IdEmployee
                                      && x.IdLeaveType == la.IdLeaveType
                                      && x.IdYear == la.IdYear
                                      && (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED")
                                  ).SumAsync(x => x.TotalLeaveDays);

                        empLeaveConfigDetailEntity.UsedLeaveDays = (int)usedLeaveDays;
                        empLeaveConfigDetailEntity.BalanceLeaveDays = empLeaveConfigDetailEntity.TotalAllocatedDays - (int)usedLeaveDays;
                        _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetailEntity);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                await _dbContext.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Leave Application approval status.");
                throw;
            }
        }

        public async Task<LeaveApplicationDetailsDto> GetLeaveApplication(int idLeaveApplication)
        {
            try
            {
                if (idLeaveApplication <= 0)
                    throw new ArgumentException("IdLeaveApplication is required.");

                var application = await _dbContext.LeaveApplications
                    .Where(x => x.IdLeaveApplication == idLeaveApplication)
                    .Select(x => new LeaveApplicationDetailsDto
                    {
                        IdLeaveApplication = x.IdLeaveApplication,
                        IdEmployee = x.IdEmployee,
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        FromDate = x.FromDate,
                        ToDate = x.ToDate,
                        IsHalfDay = x.IsHalfDay,
                        HalfDayType = x.HalfDayType,
                        TotalLeaveDays = x.TotalLeaveDays,
                        Reason = x.Reason,
                        AppliedOn = x.AppliedOn,
                        ApplicationStatus = x.ApplicationStatus,
                        CancelledDate = x.CancelledDate,
                        ReasonForCancellation = x.ReasonForCancellation,
                        ApprovalStatus = x.ApprovalStatus,
                        ApprovedBy = x.ApprovedBy,
                        IsSalaryDeducted = x.IsSalaryDeducted,
                        IdEmployeeSalary = x.IdEmployeeSalary,
                        IdSalaryMonth = x.IdSalaryMonth,
                        DeductedAmount = x.DeductedAmount
                    })
                    .FirstOrDefaultAsync();

                if (application == null)
                    throw new ArgumentException("Leave application not found.");

                var documents = await _dbContext.LeaveApplicationDocuments
                    .Where(d => d.IdLeaveApplication == idLeaveApplication)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new LeaveApplicationDocumentDetailsDto
                    {
                        IdLeaveApplicationDocument = d.IdLeaveApplicationDocument,
                        IdLeaveApplication = d.IdLeaveApplication,
                        FileType = d.FileType,
                        StoredFilePath = d.FilePath,
                        UploadedAt = d.UploadedAt,
                        FileName = null,
                        FileBinary = null
                    })
                    .ToListAsync();

                foreach (var doc in documents)
                {
                    if (!string.IsNullOrWhiteSpace(doc.StoredFilePath))
                    {
                        doc.FileName = GetOriginalFileName(doc.StoredFilePath);

                        if (File.Exists(doc.StoredFilePath))
                        {
                            doc.FileBinary = await File.ReadAllBytesAsync(doc.StoredFilePath);
                        }
                    }
                }

                application.Documents = documents ?? new List<LeaveApplicationDocumentDetailsDto>();
                return application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leave application. IdLeaveApplication: {Id}", idLeaveApplication);
                throw;
            }
        }
        private string? GetOriginalFileName(string? storedFilePath)
        {
            if (string.IsNullOrWhiteSpace(storedFilePath))
                return null;

            try
            {
                var fileName = Path.GetFileName(storedFilePath);

                if (string.IsNullOrWhiteSpace(fileName))
                    return null;

                // GUID_originalname.ext
                var underscoreIndex = fileName.IndexOf('_');

                if (underscoreIndex > 0 && underscoreIndex < fileName.Length - 1)
                {
                    var prefix = fileName.Substring(0, underscoreIndex);

                    if (Guid.TryParse(prefix, out _))
                    {
                        string originalName = fileName.Substring(underscoreIndex + 1);
                        return !string.IsNullOrWhiteSpace(originalName) ? originalName : null;
                    }
                }

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting original filename from: {StoredFilePath}", storedFilePath);
                try
                {
                    return Path.GetFileName(storedFilePath) ?? "unknown";
                }
                catch
                {
                    return "unknown";
                }
            }
        }
/*        public async Task<LeaveApplicationSaveResultDto> AddUpdateLeaveApplication_OLD(
     LeaveApplicationPostDto dto,
     int loggedInEmployeeId)
        {
            try
            {
                // Basic validations
                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                if (dto.IdLeaveType <= 0)
                    throw new ArgumentException("IdLeaveType is required.");

                if (string.IsNullOrWhiteSpace(dto.Reason))
                    throw new ArgumentException("Reason for Leave is required.");

                if (dto.ToDate.Date < dto.FromDate.Date)
                    throw new ArgumentException("ToDate cannot be earlier than FromDate.");

                dto.FromDate = dto.FromDate.Date;
                dto.ToDate = dto.ToDate.Date;
                var today = DateTime.Now.Date;

                // Get employee leave configuration (used in multiple places)
                var empLeaveConfigDetails = await GetLeaveSetupOfAnEmployee(dto.IdEmployee, null, dto.ToDate);
                if (empLeaveConfigDetails == null)
                    throw new ArgumentException("There is no Approved Leave Configuration exists for the given date");

                var empLeaveTypeConfig = empLeaveConfigDetails.Details
                    .FirstOrDefault(el => el.IdLeaveType == dto.IdLeaveType);

                if (empLeaveTypeConfig == null)
                    throw new ArgumentException("Leave type is not configured for employee.");

                // Half-day rules
                if (dto.IsHalfDay)
                {
                    if (dto.FromDate != dto.ToDate)
                        throw new ArgumentException("Half day leave must be for a single day only.");

                    if (!empLeaveTypeConfig.AllowHalfDay)
                        throw new ArgumentException("Half day is not allowed for this leave type.");

                    if (!dto.HalfDayType.HasValue)
                        throw new ArgumentException("HalfDayType is mandatory when IsHalfDay is true.");

                    var halfType = char.ToUpperInvariant(dto.HalfDayType.Value);
                    if (halfType != 'F' && halfType != 'S')
                        throw new ArgumentException("HalfDayType must be one of: F (FirstHalf), S (SecondHalf)");
                }
                else
                {
                    dto.HalfDayType = null;
                }

                // Overlap check
                var fromDate = dto.FromDate;
                var toDate = dto.ToDate.AddDays(1).AddTicks(-1);

                bool hasOverlap = await _dbContext.LeaveApplications.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee
                    && x.IdLeaveApplication != dto.IdLeaveApplication
                    && (x.ApprovalStatus != "REJECTED" && x.ApprovalStatus != "CANCELLED")
                    && x.FromDate <= toDate
                    && x.ToDate >= fromDate
                );

                if (hasOverlap)
                    throw new ArgumentException("Leave application overlaps with existing pending/approved leave.");

                // Holiday checks
                var holidaydetFrom = await _dbContext.Holidays
                    .FirstOrDefaultAsync(h => h.HolidayDate == dto.FromDate);

                if (holidaydetFrom != null)
                    throw new ArgumentException("Leave From Date is Holiday");

                var holidaydetTo = await _dbContext.Holidays
                    .FirstOrDefaultAsync(h => h.HolidayDate == dto.ToDate);

                if (holidaydetTo != null)
                    throw new ArgumentException("Leave To Date is Holiday");

                // If editing: rollback config and delete existing application
                if (dto.IdLeaveApplication > 0)
                {
                    var delentity = await _dbContext.LeaveApplications
                        .FirstOrDefaultAsync(x => x.IdLeaveApplication == dto.IdLeaveApplication);

                    if (delentity == null)
                        throw new ArgumentException("Leave application not found.");

                    if (delentity.ApprovalStatus != "SUBMITTED")
                        throw new ArgumentException("Only Submitted/Rejected leave applications can be updated.");

                    var empLeaveConfigDetail = await _dbContext.EmployeeLeaveConfigDetails
                        .FirstOrDefaultAsync(x =>
                            x.IdEmployeeLeaveConfig == empLeaveConfigDetails.IdEmployeeLeaveConfig &&
                            x.IdLeaveType == delentity.IdLeaveType &&
                            x.IdYear == delentity.IdYear);

                    if (empLeaveConfigDetail == null)
                        throw new InvalidOperationException("Employee leave configuration detail not found.");

                    // Roll back to this application's days (your requirement)
                    empLeaveConfigDetail.UsedLeaveDays = (int)delentity.TotalLeaveDays;
                    empLeaveConfigDetail.BalanceLeaveDays =
                        empLeaveConfigDetail.TotalAllocatedDays - empLeaveConfigDetail.UsedLeaveDays;

                    _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetail);

                    // Hard delete existing application
                    _dbContext.LeaveApplications.Remove(delentity);
                    await _dbContext.SaveChangesAsync();

                    // From now on, treat as NEW
                    dto.IdLeaveApplication = 0;
                }

                // Backdated rule
                if (dto.FromDate < today)
                {
                    if (!empLeaveTypeConfig.AllowBackdatedLeave)
                        throw new ArgumentException("Backdated leave is not allowed for this leave type.");

                    if (empLeaveTypeConfig.BackdateLimitDays.HasValue)
                    {
                        var daysBack = (today - dto.FromDate).Days;
                        if (daysBack > empLeaveTypeConfig.BackdateLimitDays.Value)
                            throw new ArgumentException(
                                $"Backdated leave allowed only up to {empLeaveTypeConfig.BackdateLimitDays.Value} days.");
                    }
                }

                // Compute TotalLeaveDays
                decimal totalLeaveDays = await CalculateLeaveDaysAsync(
                    empLeaveTypeConfig.IncludeHolidaysBetween,
                    dto.FromDate,
                    dto.ToDate,
                    dto.IsHalfDay);

                if (totalLeaveDays <= 0)
                    throw new ArgumentException("TotalLeaveDays is invalid after calculation.");

                if (totalLeaveDays > empLeaveTypeConfig.BalanceLeaveDays)
                    throw new ArgumentException(
                        "No enough Leave Balance. Only " +
                        empLeaveTypeConfig.BalanceLeaveDays + " days are available");

                // Monthly allowed check
                if (empLeaveTypeConfig.MaxLeavesPerMonth > 0)
                {
                    var appliedSplit = await SplitLeaveDaysByMonthAsync(dto.FromDate, dto.ToDate);
                    var alreadyTakenSplit = await GetAlreadyTakenLeavesPerMonth(
                        dto.IdEmployee,
                        dto.IdLeaveType,
                        dto.FromDate,
                        dto.ToDate);

                    foreach (var month in appliedSplit)
                    {
                        int alreadyTaken = alreadyTakenSplit.ContainsKey(month.Key)
                            ? alreadyTakenSplit[month.Key]
                            : 0;
                        int applyingNow = month.Value;
                        int totalForMonth = alreadyTaken + applyingNow;

                        if (totalForMonth > empLeaveTypeConfig.MaxLeavesPerMonth)
                        {
                            var monthName = new DateTime(month.Key.Year, month.Key.Month, 1)
                                .ToString("MMMM yyyy");

                            throw new ArgumentException(
                                $"You can apply a maximum of {empLeaveTypeConfig.MaxLeavesPerMonth} " +
                                $"leave days for the selected leave type in {monthName}. " +
                                $"Already taken: {alreadyTaken}");
                        }
                    }
                }

                // Document rule (for this simplified logic, require docs when threshold exceeded)
                if (empLeaveTypeConfig.RequiresDocument
                    && empLeaveTypeConfig.DocumentRequiredAfterDays.HasValue
                    && totalLeaveDays > empLeaveTypeConfig.DocumentRequiredAfterDays.Value)
                {
                    bool hasNewDoc = dto.Documents != null &&
                                     dto.Documents.Any(f => f != null && f.Length > 0);

                    if (!hasNewDoc)
                        throw new ArgumentException("Document is required for this leave duration. Please upload document.");
                }

                // Leave availability check
                if (empLeaveTypeConfig.BalanceLeaveDays == 0)
                    throw new ArgumentException("Insufficient leave balance for leave type." +
                                                empLeaveTypeConfig.LeaveTypeName);

                // WorkYear
                var workYear = await _dbContext.WorkYears
                    .Where(w => dto.FromDate >= w.WorkDateFrom && dto.FromDate <= w.WorkDateTo)
                    .Select(w => new
                    {
                        w.IdWorkYear,
                        w.WorkDateFrom,
                        w.WorkDateTo
                    })
                    .FirstOrDefaultAsync();

                if (workYear == null)
                    throw new ArgumentException("Invalid Date Selection");

                // Joining date check
                var emp = await _dbContext.Employees
                    .Where(ee => ee.IdEmployee == dto.IdEmployee)
                    .Select(e => new { e.JoiningDate })
                    .FirstOrDefaultAsync();

                if (emp != null && dto.FromDate < emp.JoiningDate)
                    throw new ArgumentException("You cannot apply for leave for a date, before your Joining Date");

                // Always create new entity now (we deleted old one on edit)
                var approvalDetails = await GetApprovalLevelDetails(
                    empLeaveTypeConfig.IdLeaveTemplateDetail,
                    dto.IdEmployee,
                    loggedInEmployeeId) ?? "";

                var entity = new LeaveApplications
                {
                    IdEmployee = dto.IdEmployee,
                    IdLeaveType = dto.IdLeaveType,
                    LeaveTypeName = dto.LeaveTypeName,
                    FromDate = dto.FromDate,
                    ToDate = dto.ToDate,
                    IsHalfDay = dto.IsHalfDay,
                    HalfDayType = dto.HalfDayType,
                    TotalLeaveDays = totalLeaveDays,
                    Reason = dto.Reason.Trim(),
                    AppliedOn = DateTime.Now,
                    ApprovalStatus = "SUBMITTED",
                    IdLeaveTemplateDetail = empLeaveTypeConfig.IdLeaveTemplateDetail,
                    IdYear = workYear.IdWorkYear,
                    LeaveApprovalDetails = approvalDetails
                };

                await _dbContext.LeaveApplications.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                // Save documents for this application
                if (dto.Documents != null && dto.Documents.Any(f => f != null && f.Length > 0))
                {
                    foreach (var file in dto.Documents)
                    {
                        if (file == null || file.Length == 0) continue;

                        var savedPath = await SaveLeaveApplicationFileAsync(file);

                        var docEntity = new LeaveApplicationDocuments
                        {
                            IdLeaveApplication = entity.IdLeaveApplication,
                            FileType = Path.GetExtension(file.FileName).Replace(".", "").ToUpperInvariant(),
                            FileName = file.FileName,
                            FilePath = savedPath,
                            UploadedAt = DateTime.Now
                        };

                        await _dbContext.LeaveApplicationDocuments.AddAsync(docEntity);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                // Recalculate used/balance from table
                var usedLeaveDays = await _dbContext.LeaveApplications
                    .Where(x =>
                        x.IdEmployee == dto.IdEmployee &&
                        x.IdLeaveType == dto.IdLeaveType &&
                        x.IdYear == workYear.IdWorkYear &&
                        (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED"))
                    .SumAsync(x => x.TotalLeaveDays);

                var empLeaveConfigDetailEntity = await _dbContext.EmployeeLeaveConfigDetails
                    .FirstOrDefaultAsync(x =>
                        x.IdEmployeeLeaveConfig == empLeaveConfigDetails.IdEmployeeLeaveConfig &&
                        x.IdLeaveType == dto.IdLeaveType);

                if (empLeaveConfigDetailEntity == null)
                    throw new InvalidOperationException("Employee leave configuration detail not found.");

                empLeaveConfigDetailEntity.UsedLeaveDays = (int)usedLeaveDays;
                empLeaveConfigDetailEntity.BalanceLeaveDays =
                    empLeaveConfigDetailEntity.TotalAllocatedDays - empLeaveConfigDetailEntity.UsedLeaveDays;

                _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetailEntity);
                await _dbContext.SaveChangesAsync();

                // Update day attendance for this application
                UpdateDayAttendanceStatus(dto.IdEmployee,
                    entity.IdLeaveApplication,
                    dto.FromDate,
                    dto.ToDate,
                    "SUBMITTED");

                await _approvalWorkflowService.InitiateApprovalWorkflow(
                    entity.IdLeaveApplication,
                    "LEAVE_" + empLeaveConfigDetailEntity.IdLeaveTemplateDetail,
                    entity.IdEmployee,
                    "APPROVED",
                    0,
                    "APPROVED",
                    1);

                return new LeaveApplicationSaveResultDto
                {
                    SuccessFlag = true,
                    Message = "Leave application saved successfully.",
                    IdLeaveApplication = entity.IdLeaveApplication,
                    TotalLeaveDays = entity.TotalLeaveDays,
                    ApprovalStatus = entity.ApprovalStatus,
                    ApplicationStatus = entity.ApplicationStatus,
                    AppliedOn = entity.AppliedOn
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        */
        public async Task<LeaveApplicationSaveResultDto> AddUpdateLeaveApplication(LeaveApplicationPostDto dto, int loggedInEmployeeId)
        {
            try
            {
                // Basic validations
                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                if (dto.IdLeaveType <= 0)
                    throw new ArgumentException("IdLeaveType is required.");

                if (string.IsNullOrWhiteSpace(dto.Reason))
                    throw new ArgumentException("Reason for Leave is required.");

                if (dto.ToDate.Date < dto.FromDate.Date)
                    throw new ArgumentException("ToDate cannot be earlier than FromDate.");

                dto.FromDate = dto.FromDate.Date;
                dto.ToDate = dto.ToDate.Date;

                var today = DateTime.Now.Date;

                // Get leave config first (needed in delete block and later)
                var empLeaveConfigDetails = await GetLeaveSetupOfAnEmployee(dto.IdEmployee, null, dto.ToDate);
                if (empLeaveConfigDetails == null)
                    throw new ArgumentException("There is no Approved Leave Configuration exists for the given date");

                var empLeaveTypeConfig = empLeaveConfigDetails.Details
                    .FirstOrDefault(el => el.IdLeaveType == dto.IdLeaveType);

                if (empLeaveTypeConfig == null)
                    throw new ArgumentException("Leave type is not configured for employee.");

                // Half-day rules
                if (dto.IsHalfDay)
                {
                    if (dto.FromDate != dto.ToDate)
                        throw new ArgumentException("Half day leave must be for a single day only.");

                    if (!empLeaveTypeConfig.AllowHalfDay)
                        throw new ArgumentException("Half day is not allowed for this leave type.");

                    if (!dto.HalfDayType.HasValue)
                        throw new ArgumentException("HalfDayType is mandatory when IsHalfDay is true.");

                    var halfType = char.ToUpperInvariant(dto.HalfDayType.Value);
                    if (halfType != 'F' && halfType != 'S')
                        throw new ArgumentException("HalfDayType must be one of: F (FirstHalf), S (SecondHalf)");
                }
                else
                {
                    dto.HalfDayType = null;
                }

                // Check overlap
                var fromDate = dto.FromDate;
                var toDate = dto.ToDate.AddDays(1).AddTicks(-1);

                bool hasOverlap = await _dbContext.LeaveApplications.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee
                    && x.IdLeaveApplication != dto.IdLeaveApplication
                    && (x.ApprovalStatus != "REJECTED" && x.ApprovalStatus != "CANCELLED")
                    && x.FromDate <= toDate
                    && x.ToDate >= fromDate);

                if (hasOverlap)
                    throw new ArgumentException("Leave application overlaps with existing pending/approved leave.");

                // Holiday checks
                var holidaydetFrom = await _dbContext.Holidays
                    .FirstOrDefaultAsync(h => h.HolidayDate == dto.FromDate);
                if (holidaydetFrom != null)
                    throw new ArgumentException("Leave From Date is Holiday");

                var holidaydetTo = await _dbContext.Holidays
                    .FirstOrDefaultAsync(h => h.HolidayDate == dto.ToDate);
                if (holidaydetTo != null)
                    throw new ArgumentException("Leave To Date is Holiday");

                // If edit: rollback config and delete old application
                if (dto.IdLeaveApplication > 0)
                {
                    var delentity = await _dbContext.LeaveApplications
                        .FirstOrDefaultAsync(x => x.IdLeaveApplication == dto.IdLeaveApplication);

                    if (delentity == null)
                        throw new ArgumentException("Leave application not found.");

                    if (delentity.ApprovalStatus != "SUBMITTED")
                        throw new ArgumentException("Only Submitted/Rejected leave applications can be updated.");

                    var empLeaveConfigDetail = await _dbContext.EmployeeLeaveConfigDetails
                        .FirstOrDefaultAsync(x =>
                            x.IdEmployeeLeaveConfig == empLeaveConfigDetails.IdEmployeeLeaveConfig &&
                            x.IdLeaveType == delentity.IdLeaveType &&
                            x.IdYear == delentity.IdYear);

                    if (empLeaveConfigDetail == null)
                        throw new InvalidOperationException("Employee leave configuration detail not found.");

                    // Rollback to old application's usage
                    empLeaveConfigDetail.UsedLeaveDays = (int)delentity.TotalLeaveDays;
                    empLeaveConfigDetail.BalanceLeaveDays =
                        empLeaveConfigDetail.TotalAllocatedDays - empLeaveConfigDetail.UsedLeaveDays;

                    _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetail);

                    // Delete existing application
                    _dbContext.LeaveApplications.Remove(delentity);

                    await _dbContext.SaveChangesAsync();

                    // From here we always treat as NEW
                    dto.IdLeaveApplication = 0;
                }

                // Backdated rules
                if (dto.FromDate < today)
                {
                    if (!empLeaveTypeConfig.AllowBackdatedLeave)
                        throw new ArgumentException("Backdated leave is not allowed for this leave type.");

                    if (empLeaveTypeConfig.BackdateLimitDays.HasValue)
                    {
                        var daysBack = (today - dto.FromDate).Days;
                        if (daysBack > empLeaveTypeConfig.BackdateLimitDays.Value)
                            throw new ArgumentException(
                                $"Backdated leave allowed only up to {empLeaveTypeConfig.BackdateLimitDays.Value} days.");
                    }
                }

                // Compute TotalLeaveDays
                decimal totalLeaveDays = await CalculateLeaveDaysAsync(
                    empLeaveTypeConfig.IncludeHolidaysBetween,
                    dto.FromDate,
                    dto.ToDate,
                    dto.IsHalfDay);

                if (totalLeaveDays <= 0)
                    throw new ArgumentException("TotalLeaveDays is invalid after calculation.");

                if (totalLeaveDays > empLeaveTypeConfig.BalanceLeaveDays)
                    throw new ArgumentException(
                        "No enough Leave Balance. Only " +
                        empLeaveTypeConfig.BalanceLeaveDays + " days are available");

                // Monthly allowed
                if (empLeaveTypeConfig.MaxLeavesPerMonth > 0)
                {
                    var appliedSplit = await SplitLeaveDaysByMonthAsync(dto.FromDate, dto.ToDate);
                    var alreadyTakenSplit = await GetAlreadyTakenLeavesPerMonth(dto.IdEmployee, dto.IdLeaveType, dto.FromDate, dto.ToDate);

                    foreach (var month in appliedSplit)
                    {
                        int alreadyTaken = alreadyTakenSplit.ContainsKey(month.Key)
                            ? alreadyTakenSplit[month.Key]
                            : 0;

                        int applyingNow = month.Value;
                        int totalForMonth = alreadyTaken + applyingNow;

                        if (totalForMonth > empLeaveTypeConfig.MaxLeavesPerMonth)
                        {
                            var monthName = new DateTime(month.Key.Year, month.Key.Month, 1)
                                .ToString("MMMM yyyy");

                            throw new ArgumentException(
                                $"You can apply a maximum of {empLeaveTypeConfig.MaxLeavesPerMonth} " +
                                $"leave days for the selected leave type in {monthName}. " +
                                $"Already taken: {alreadyTaken}");
                        }
                    }
                }

                // Document rule
                if (empLeaveTypeConfig.RequiresDocument
                    && empLeaveTypeConfig.DocumentRequiredAfterDays.HasValue
                    && totalLeaveDays > empLeaveTypeConfig.DocumentRequiredAfterDays.Value)
                {
                    bool hasNewDoc = dto.Documents != null &&
                                     dto.Documents.Any(f => f != null && f.Length > 0);

                    if (!hasNewDoc)
                        throw new ArgumentException("Document is required for this leave duration. Please upload document.");
                }

                // Leave availability simple check
                if (empLeaveTypeConfig.BalanceLeaveDays == 0)
                    throw new ArgumentException("Insufficient leave balance for leave type." +
                                                empLeaveTypeConfig.LeaveTypeName);

                // Work year
                var workYear = await _dbContext.WorkYears
                    .Where(w => dto.FromDate >= w.WorkDateFrom && dto.FromDate <= w.WorkDateTo)
                    .Select(w => new
                    {
                        w.IdWorkYear,
                        w.WorkDateFrom,
                        w.WorkDateTo
                    })
                    .FirstOrDefaultAsync();

                if (workYear == null)
                    throw new ArgumentException("Invalid Date Selection");

                // Joining date check
                var emp = await _dbContext.Employees
                    .Where(ee => ee.IdEmployee == dto.IdEmployee)
                    .Select(e => new { e.JoiningDate })
                    .FirstOrDefaultAsync();

                if (emp != null && dto.FromDate < emp.JoiningDate)
                    throw new ArgumentException("You cannot apply for leave for a date, before your Joining Date");

                // Create new application (we always reach here with dto.IdLeaveApplication == 0)
                var approvalDetails = await GetApprovalLevelDetails(
                    empLeaveTypeConfig.IdLeaveTemplateDetail,
                    dto.IdEmployee,
                    loggedInEmployeeId) ?? "";

                var entity = new LeaveApplications
                {
                    IdEmployee = dto.IdEmployee,
                    IdLeaveType = dto.IdLeaveType,
                    LeaveTypeName = dto.LeaveTypeName,
                    FromDate = dto.FromDate,
                    ToDate = dto.ToDate,
                    IsHalfDay = dto.IsHalfDay,
                    HalfDayType = dto.HalfDayType,
                    TotalLeaveDays = totalLeaveDays,
                    Reason = dto.Reason.Trim(),
                    AppliedOn = DateTime.Now,
                    ApprovalStatus = "SUBMITTED",
                    IdLeaveTemplateDetail = empLeaveTypeConfig.IdLeaveTemplateDetail,
                    IdYear = workYear.IdWorkYear,
                    LeaveApprovalDetails = approvalDetails
                };

                await _dbContext.LeaveApplications.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                // Save documents (for new ID)
                if (dto.Documents != null && dto.Documents.Any(f => f != null && f.Length > 0))
                {
                    foreach (var file in dto.Documents)
                    {
                        if (file == null || file.Length == 0) continue;

                        var savedPath = await SaveLeaveApplicationFileAsync(file);

                        var docEntity = new LeaveApplicationDocuments
                        {
                            IdLeaveApplication = entity.IdLeaveApplication,
                            FileType = Path.GetExtension(file.FileName).Replace(".", "").ToUpperInvariant(),
                            FileName = file.FileName,
                            FilePath = savedPath,
                            UploadedAt = DateTime.Now
                        };

                        await _dbContext.LeaveApplicationDocuments.AddAsync(docEntity);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                // Recalculate used leaves from table and update config detail
                var usedLeaveDays = await _dbContext.LeaveApplications
                    .Where(x =>
                        x.IdEmployee == dto.IdEmployee &&
                        x.IdLeaveType == dto.IdLeaveType &&
                        x.IdYear == workYear.IdWorkYear &&
                        (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED"))
                    .SumAsync(x => x.TotalLeaveDays);

                var empLeaveConfigDetailEntity = await _dbContext.EmployeeLeaveConfigDetails
                    .FirstOrDefaultAsync(x =>
                        x.IdEmployeeLeaveConfig == empLeaveConfigDetails.IdEmployeeLeaveConfig &&
                        x.IdLeaveType == dto.IdLeaveType);

                if (empLeaveConfigDetailEntity == null)
                    throw new InvalidOperationException("Employee leave configuration detail not found.");

                empLeaveConfigDetailEntity.UsedLeaveDays = (int)usedLeaveDays;
                empLeaveConfigDetailEntity.BalanceLeaveDays =
                    empLeaveConfigDetailEntity.TotalAllocatedDays - empLeaveConfigDetailEntity.UsedLeaveDays;

                _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetailEntity);
                await _dbContext.SaveChangesAsync();

                // Update Day Attendance (for new application id)
                UpdateDayAttendanceStatus(dto.IdEmployee,
                    entity.IdLeaveApplication,
                    dto.FromDate,
                    dto.ToDate,
                    "SUBMITTED");

                // Approval workflow
                await _approvalWorkflowService.InitiateApprovalWorkflow(
                    entity.IdLeaveApplication,
                    "LEAVE_" + empLeaveConfigDetailEntity.IdLeaveTemplateDetail,
                    entity.IdEmployee,
                    "APPROVED",
                    0,
                    "APPROVED",
                    1);

                return new LeaveApplicationSaveResultDto
                {
                    SuccessFlag = true,
                    Message = "Leave application saved successfully.",
                    IdLeaveApplication = entity.IdLeaveApplication,
                    TotalLeaveDays = entity.TotalLeaveDays,
                    ApprovalStatus = entity.ApprovalStatus,
                    ApplicationStatus = entity.ApplicationStatus,
                    AppliedOn = entity.AppliedOn
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UpdateDayAttendanceStatus(int IdEmployee, int IdLeaveApplication, DateTime LeaveFromDate, DateTime LeaveToDate, string LeaveStatus)
        {
            string leaveStatusDetails;

            if (LeaveStatus == "SUBMITTED" || LeaveStatus.Contains("APPROVE"))
            {
                leaveStatusDetails = "Authorized Absence";
            }
            else if (LeaveStatus == "CANCELLED" || LeaveStatus.Contains("REJECTED"))
            {
                leaveStatusDetails = "UnAuthorized Absence";
            }
            else
            {
                leaveStatusDetails = LeaveStatus; // fallback safety
            }

            using var con = (SqlConnection)_dbContext.Database.GetDbConnection();
            using var cmd = new SqlCommand(@"
                        UPDATE DayAttendance
                        SET StatusDetails = CONCAT('Leave ', @StatusDetails),
                            IdEmployeeLeave = @IdLeaveApplication,
                            IsLeaveApplied = 1
                        WHERE IdEmployee = @IdEmployee
                          AND FirstInDateTime IS NULL
                          AND LastOutDateTime IS NULL
                          AND AttendanceDate BETWEEN @FromDate AND @ToDate", con);

            cmd.Parameters.AddWithValue("@StatusDetails", leaveStatusDetails);
            cmd.Parameters.AddWithValue("@IdLeaveApplication", IdLeaveApplication);
            cmd.Parameters.AddWithValue("@IdEmployee", IdEmployee);
            cmd.Parameters.AddWithValue("@FromDate", LeaveFromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", LeaveToDate.Date);

            if (con.State != ConnectionState.Open)
                con.Open();

            cmd.ExecuteNonQuery();
        }

        private async Task<Dictionary<(int Year, int Month), int>> SplitLeaveDaysByMonthAsync(
           DateTime fromDate, DateTime toDate)
        {
            fromDate = fromDate.Date;
            toDate = toDate.Date;

            // 1. Load holidays within the range (only date part)
            var holidays = await _dbContext.Holidays
                .Where(h => h.HolidayDate >= fromDate && h.HolidayDate <= toDate)
                .Select(h => h.HolidayDate.Date)
                .ToListAsync();

            var holidaySet = new HashSet<DateTime>(holidays);

            // 2. Split leave days by month, skipping holidays
            var result = new Dictionary<(int Year, int Month), int>();
            var current = fromDate;

            while (current <= toDate)
            {
                if (holidaySet.Contains(current))
                {
                    current = current.AddDays(1);
                    continue; // skip holiday
                }

                var key = (current.Year, current.Month);

                if (!result.ContainsKey(key))
                    result[key] = 0;

                result[key]++;

                current = current.AddDays(1);
            }

            return result;
        }


        private async Task<Dictionary<(int Year, int Month), int>> GetAlreadyTakenLeavesPerMonth(int employeeId, int leaveTypeId, DateTime fromDate, DateTime toDate)
        {

            var monthStart = new DateTime(fromDate.Year, fromDate.Month, 1);
            var monthEnd = new DateTime(toDate.Year, toDate.Month,
                                          DateTime.DaysInMonth(toDate.Year, toDate.Month));


            var leaves = await _dbContext.LeaveApplications.Where(l =>
                         l.IdEmployee == employeeId && l.IdLeaveType == leaveTypeId &&
                         (l.ApprovalStatus == "APPROVED" || l.ApprovalStatus == "SUBMITTED") &&
                         l.ToDate >= monthStart && l.FromDate <= monthEnd).AsNoTracking().ToListAsync();

            var result = new Dictionary<(int Year, int Month), int>();

            foreach (var leave in leaves)
            {
                DateTime current = leave.FromDate.Date;
                DateTime end = leave.ToDate.Date;

                while (current <= end)
                {
                    var key = (current.Year, current.Month);

                    if (!result.ContainsKey(key))
                        result[key] = 0;

                    result[key]++;
                    current = current.AddDays(1);
                }
            }

            return result;
        }

        public async Task<bool> DeleteLeaveApplicationDocument(int idLeaveApplicationDocument, int loggedInEmployeeId, bool isHrOverride)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var doc = await _dbContext.LeaveApplicationDocuments
                    .FirstOrDefaultAsync(x => x.IdLeaveApplicationDocument == idLeaveApplicationDocument);

                if (doc == null)
                    throw new ArgumentException("Leave application document not found.");

                var application = await _dbContext.LeaveApplications
                    .FirstOrDefaultAsync(x => x.IdLeaveApplication == doc.IdLeaveApplication);

                if (application == null)
                    throw new ArgumentException("Leave application not found.");

                // ✅ Rule: allow delete only when Pending or SentBack
                // ✅ If Approved → allow only HR override
                var approval = application.ApprovalStatus?.Trim().ToUpperInvariant();
                var appStatus = application.ApplicationStatus?.Trim().ToUpperInvariant();

                bool isPending = approval == "PENDING";
                bool isSentBack = appStatus == "REJECTED";
                bool isApproved = approval == "APPROVED";

                if (isApproved && !isHrOverride)
                    throw new ArgumentException("Document cannot be deleted after approval. HR override required.");

                if (!isApproved && !isPending && !isSentBack)
                    throw new ArgumentException("Document can be deleted only when application is Pending or Rejected.");

                // ✅ Delete physical file
                if (!string.IsNullOrWhiteSpace(doc.FilePath) && File.Exists(doc.FilePath))
                {
                    File.Delete(doc.FilePath);
                }

                // ✅ Remove DB record
                _dbContext.LeaveApplicationDocuments.Remove(doc);
                await _dbContext.SaveChangesAsync();

                // ✅ Audit Log (recommended)
                // await _auditService.LogAsync("LeaveDocumentDeleted", loggedInEmployeeId, ...);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> SaveLeaveApplicationFileAsync(IFormFile file, string? oldFilePath = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            try
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "leaveapplications");
                Directory.CreateDirectory(folderPath);

                // ✅ delete old file if exists (optional)
                if (!string.IsNullOrWhiteSpace(oldFilePath))
                {
                    string fullOldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldFilePath.TrimStart('/').Replace("/", "\\"));
                    if (File.Exists(fullOldPath))
                    {
                        File.Delete(fullOldPath);
                    }
                }

                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string fullFilePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verify file was created
                if (!File.Exists(fullFilePath))
                    throw new InvalidOperationException("File was not saved successfully.");

                // ✅ Store relative path instead of absolute path
                string relativePath = Path.Combine("uploads", "leaveapplications", fileName).Replace("\\", "/");
                _logger.LogInformation("File saved successfully: {RelativePath}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving leave application file: {FileName}", file?.FileName);
                throw;
            }
        }

        private string? ConvertPathToUrl(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            try
            {
                // Check if file exists first
                string fullPath = filePath;

                // If it's a relative path, construct full path
                if (!Path.IsPathRooted(filePath))
                {
                    // Ensure the file path starts from 'wwwroot' directory
                    fullPath = Path.Combine(_env?.WebRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/').Replace("/", "\\"));
                }

                // Check file existence
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found at path: {FilePath}", fullPath);
                    return null;
                }

                // Fallback: just normalize slashes
                return fullPath.Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting path to URL: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<decimal> CalculateLeaveDaysAsync(bool IncludeHoliday, DateTime fromDate, DateTime toDate, bool isHalfDay)
        {
            if (isHalfDay)
                return 0.5m;

            // ✅ Fetch holidays only once
            var holidayDates = await _dbContext.Holidays
                .Where(h => h.HolidayDate.Date >= fromDate.Date && h.HolidayDate.Date <= toDate.Date)
                .Select(h => h.HolidayDate.Date)
                .ToListAsync();

            var holidaySet = new HashSet<DateTime>(holidayDates);

            int count = 0;

            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                //// ✅ exclude weekend
                //if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                //    continue;

                // ✅ exclude holiday

                if (holidaySet.Contains(date) && IncludeHoliday == false)
                    continue;

                count++;
            }

            return count;
        }

        public async Task<CancelLeaveApplicationResultDto> CancelLeaveApplication(int idLeaveApplication, string? cancelReason, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var application = await _dbContext.LeaveApplications
                    .FirstOrDefaultAsync(x => x.IdLeaveApplication == idLeaveApplication);

                if (application == null)
                    throw new ArgumentException("Leave application not found.");

                // ✅ Employee can cancel only own leave (unless HR override is handled in controller)
                if (application.IdEmployee != loggedInEmployeeId)
                    throw new ArgumentException("You can cancel only your own leave application.");

                var approvalStatus = application.ApprovalStatus?.Trim().ToUpperInvariant();
                var appStatus = application.ApplicationStatus?.Trim().ToUpperInvariant();

                if (application.FromDate < DateTime.Now)
                    throw new ArgumentException("Cancellation of past-dated leave applications is not allowed.");

                // ✅ Cancel the leave
                application.ApplicationStatus = "Cancelled";
                application.ApprovalStatus = "CANCELLED";
                application.CancelledDate = DateTime.Now;
                application.ReasonForCancellation = string.IsNullOrWhiteSpace(cancelReason)
                    ? "Cancelled by employee"
                    : cancelReason.Trim();

                await _dbContext.SaveChangesAsync();

                var empLeaveConfig = await _dbContext.EmployeeLeaveConfigs
                  .FirstOrDefaultAsync(x => x.IdEmployee == application.IdEmployee &&
                      application.FromDate >= x.EffectiveFrom && (x.EffectiveTo == null || application.FromDate <= x.EffectiveTo));
                //Update Used and Balance details in employee leave config detail
                var empLeaveConfigDetailEntity = await _dbContext.EmployeeLeaveConfigDetails
                    .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == empLeaveConfig.IdEmployeeLeaveConfig && x.IdLeaveType == application.IdLeaveType);

                if (empLeaveConfigDetailEntity == null)
                    throw new InvalidOperationException("Employee leave configuration detail not found.");
                var usedLeaveDays = await _dbContext.LeaveApplications
                          .Where(x =>
                              x.IdEmployee == application.IdEmployee
                              && x.IdLeaveType == application.IdLeaveType
                              && x.IdYear == application.IdYear
                              && (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED")
                          ).SumAsync(x => x.TotalLeaveDays);

                empLeaveConfigDetailEntity.UsedLeaveDays = (int)usedLeaveDays;
                empLeaveConfigDetailEntity.BalanceLeaveDays = empLeaveConfigDetailEntity.TotalAllocatedDays - (int)usedLeaveDays;
                _dbContext.EmployeeLeaveConfigDetails.Update(empLeaveConfigDetailEntity);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CancelLeaveApplicationResultDto
                {
                    SuccessFlag = true,
                    Message = "Leave application cancelled successfully.",
                    IdLeaveApplication = application.IdLeaveApplication,
                    ApplicationStatus = application.ApplicationStatus,
                    CancelledDate = application.CancelledDate,
                    ReasonForCancellation = application.ReasonForCancellation
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<LeaveDashboardDto>> GetLeaveDashboardEmployee(int idEmployee, int idYear)
        {
            // ===============================
            // STEP 1: FETCH RAW DATA (NO GROUP BY)
            // ===============================
            var result = await
            (
                from ec in _dbContext.EmployeeLeaveConfigs
                join ecd in _dbContext.EmployeeLeaveConfigDetails
                    on ec.IdEmployeeLeaveConfig equals ecd.IdEmployeeLeaveConfig
                join ltd in _dbContext.LeaveTemplateDetails on ecd.IdLeaveTemplateDetail equals ltd.IdLeaveTemplateDetails
                where ec.IdEmployee == idEmployee
                      && ltd.IdYear == idYear && ecd.TotalAllocatedDays >0

                select new LeaveDashboardDto
                {
                    IdEmployee = ec.IdEmployee,
                    IdLeaveType = ecd.IdLeaveType,
                    LeaveTypeCode = ltd.LeaveCode,
                    LeaveTypeName = ltd.LeaveTypeName,
                    TotalAllocated = ecd.TotalAllocatedDays,
                    TotalTaken = ecd.UsedLeaveDays,
                    TotalApproved = 0,
                    TotalRejected = 0,
                    TotalBalance = ecd.BalanceLeaveDays
                }
            ).ToListAsync();
            return result;
        }

        public async Task<List<MonthlyLeaveDashboardDto>>GetLeaveDashboardEmployeeMonthWise(int idEmployee, int idYear)
        {

            var query =
                from ec in _dbContext.EmployeeLeaveConfigs
                join ecd in _dbContext.EmployeeLeaveConfigDetails
                    on ec.IdEmployeeLeaveConfig equals ecd.IdEmployeeLeaveConfig

                join la in _dbContext.LeaveApplications
                    on new { ec.IdEmployee, ecd.IdLeaveType, IdYear = idYear }
                 equals new { la.IdEmployee, la.IdLeaveType, IdYear = la.IdYear ?? 0 }
                    into laGroup
                from la in laGroup.DefaultIfEmpty()

                where ec.IdEmployee == idEmployee
                      && ec.EffectiveFrom.Year == idYear

                group la by new
                {
                    ec.IdEmployee,
                    ecd.IdLeaveType,
                    ecd.TotalAllocatedDays,

                    Month = la != null ? la.FromDate.Month : 0,
                    LeaveTypeName = la.LeaveTypeName
                }
                into g
                where g.Key.Month != 0   // remove empty month rows
                select new MonthlyLeaveDashboardDto
                {
                    IdEmployee = g.Key.IdEmployee,
                    IdLeaveType = g.Key.IdLeaveType,
                    LeaveTypeName = g.Key.LeaveTypeName ?? "N/A",

                    Month = g.Key.Month,
                    MonthName = System.Globalization.CultureInfo
                                    .CurrentCulture
                                    .DateTimeFormat
                                    .GetAbbreviatedMonthName(g.Key.Month),

                    TotalTaken = g.Sum(x =>
                        x != null ? x.TotalLeaveDays : 0),

                    TotalApproved = g.Sum(x =>
                        x != null && x.ApprovalStatus == "APPROVED"
                            ? x.TotalLeaveDays
                            : 0),

                    TotalRejected = g.Sum(x =>
                        x != null && x.ApprovalStatus == "REJECTED"
                            ? x.TotalLeaveDays
                            : 0),

                    TotalBalance =
                        g.Key.TotalAllocatedDays -
                        g.Sum(x =>
                            x != null &&
                            (x.ApprovalStatus == "APPROVED" || x.ApprovalStatus == "SUBMITTED")
                                ? x.TotalLeaveDays
                                : 0)
                };

            return await query
                .OrderBy(x => x.Month)
                .ThenBy(x => x.IdLeaveType)
                .ToListAsync();
        }

        public async Task<List<LeaveApplicationListDto>> GetLeaveApplicationsEmployee(int idEmployee, DateTime DateFrom, DateTime DateTo)
        {
            var query =
                from la in _dbContext.LeaveApplications
                join emp in _dbContext.Employees
                    on la.IdEmployee equals emp.IdEmployee into empGroup
                from emp in empGroup.DefaultIfEmpty()

                join dept in _dbContext.Departments
                    on emp.IdDepartment equals dept.IdDepartment into deptGroup
                from dept in deptGroup.DefaultIfEmpty()

                join desig in _dbContext.Designations
                    on emp.IdDesignation equals desig.IdDesignation into desigGroup
                from desig in desigGroup.DefaultIfEmpty()

                where la.IdEmployee == idEmployee
                      && la.FromDate >= DateFrom && la.FromDate <= DateTo

                orderby la.AppliedOn descending

                select new LeaveApplicationListDto
                {
                    IdLeaveApplication = la.IdLeaveApplication,
                    IdEmployee = la.IdEmployee,

                    EmployeeName = emp == null ? null
                    : (emp.FirstName ?? "") +
                      ((emp.FirstName != null && emp.LastName != null) ? " " : "") +
                      (emp.LastName ?? ""),
                    DepartmentName = dept != null ? dept.DepartmentName : null,
                    DesignationName = desig != null ? desig.DesignationName : null,

                    IdLeaveTemplateDetail = la.IdLeaveTemplateDetail,
                    IdLeaveType = la.IdLeaveType,
                    LeaveTypeName = la.LeaveTypeName,
                    Reason = la.Reason,
                    IsHalfDay = la.IsHalfDay,
                    HalfDayType = la.HalfDayType,
                    FromDate = la.FromDate,
                    ToDate = la.ToDate,
                    TotalLeaveDays = la.TotalLeaveDays,

                    ApprovalStatus = la.ApprovalStatus,
                    ApplicationStatus = la.ApplicationStatus,
                    AppliedOn = la.AppliedOn
                };
            return await query.ToListAsync();
        }

        public async Task<List<LeaveApplicationDocumentDto>> GetLeaveApplicationDocuments(int idLeaveApplication)
        {
            try
            {
                var records = await _dbContext.LeaveApplicationDocuments
                .Where(d => d.IdLeaveApplication == idLeaveApplication)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new
                {
                    d.IdLeaveApplicationDocument,
                    d.FileType,
                    d.FileName,
                    d.FilePath,
                    d.UploadedAt
                })
                .ToListAsync(); // 👈 materialize here
                var documents = new List<LeaveApplicationDocumentDto>();

                foreach (var record in records)
                {
                    var documentDto = new LeaveApplicationDocumentDto
                    {
                        IdLeaveApplicationDocument = record.IdLeaveApplicationDocument,
                        FileType = record.FileType,
                        FileName = record.FileName,
                        FileUrl = ConvertPathToUrl(record.FilePath), // Convert path to URL
                        UploadedAt = record.UploadedAt
                    };

                    // Check if the file exists and convert to binary (byte array)
                    string filePath = documentDto.FileUrl;
                    if (File.Exists(filePath))
                    {
                        documentDto.FileBinary = await File.ReadAllBytesAsync(filePath); // Converting file to binary
                    }

                    documents.Add(documentDto);
                }



                return documents;
            }
            catch (Exception ee)
            {
                int pp = 0;
                _logger.LogError(ee, "Error fetching Leave Application Documents");
                throw new ArgumentException(ee.Message);
            }
        }

        #endregion

        #region 
        public async Task<TodayAtAGlanceDto> GetTodaySummaryAsync(DateTime date)
        {
            var today = date.Date;

            // Approved leave (any type) overlapping today
            var onLeaveIds = await _dbContext.LeaveApplications
                .Where(l =>
                    l.ApprovalStatus == "APPROVED" &&
                    l.FromDate <= today &&
                    l.ToDate >= today)
                .Select(l => (int?)l.IdEmployee)   // force int?
                .Distinct()
                .ToListAsync();

            // Work from home
            var wfhIds = await _dbContext.LeaveApplications
                .Where(l =>
                    l.ApprovalStatus == "APPROVED" &&
                    l.LeaveTypeName == "Work From Home" &&
                    l.FromDate <= today &&
                    l.ToDate >= today)
                .Select(l => (int?)l.IdEmployee)
                .Distinct()
                .ToListAsync();

            // Clocked in today
            var clockedIds = await _dbContext.ClockInOutDetails
                .Where(c => c.ClockDate.Date == today)
                .Select(c => (int?)c.IdEmployee)
                .Distinct()
                .ToListAsync();

            // Active employees
            var activeEmployees = await _dbContext.Employees
                .Where(e => e.CurrentStatus == "Working")
                .Select(e => (int?)e.IdEmployee)
                .ToListAsync();

            // Use HashSet<int?> so Contains takes int?
            var onLeaveSet = onLeaveIds.ToHashSet();     // HashSet<int?>
            var clockedSet = clockedIds.ToHashSet();     // HashSet<int?>
            var wfhSet = wfhIds.ToHashSet();         // HashSet<int?>

            // Unauthorized absent: active employees NOT on leave AND NOT clocked today
            var unauthorizedAbsent = activeEmployees
                .Count(id => id.HasValue &&
                             !onLeaveSet.Contains(id) &&
                             !clockedSet.Contains(id));

            return new TodayAtAGlanceDto
            {
                Date = today,
                OnLeaveToday = onLeaveSet.Count(id => id.HasValue),
                UnauthorizedAbsentToday = unauthorizedAbsent,
                WorkFromHomeToday = wfhSet.Count(id => id.HasValue)
            };
        }


        public async Task<List<LeaveRequestRowDto>> GetLeaveRequestsAsync(
            DateTime? from, DateTime? to, int? idDepartment, int? idDesignation, string status, string search)
        {
            var query =
                from la in _dbContext.LeaveApplications
                join e in _dbContext.Employees on la.IdEmployee equals e.IdEmployee
                join d in _dbContext.Departments on e.IdDepartment equals d.IdDepartment
                join g in _dbContext.Designations on e.IdDesignation equals g.IdDesignation
                select new { la, e, d, g };

            if (from.HasValue)
                query = query.Where(x => x.la.FromDate >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(x => x.la.ToDate <= to.Value.Date);

            if (idDepartment.HasValue)
                query = query.Where(x => x.e.IdDepartment == idDepartment.Value);

            if (idDesignation.HasValue)
                query = query.Where(x => x.e.IdDesignation == idDesignation.Value);

            if (status.ToUpper() != "ALL")
            {
                if (status.ToUpper() == "SUBMITTED")
                {
                    query = query.Where(x => x.la.ApprovalStatus != "APPROVED" && x.la.ApprovalStatus != "REJECTED" &&
                         x.la.ApprovalStatus != "CANCELLED");
                }
                if (status.ToUpper() == "APPROVED")
                {
                    query = query.Where(x => x.la.ApprovalStatus == "APPROVED");
                }
                if (status.ToUpper() == "REJECTED")
                {
                    query = query.Where(x => x.la.ApprovalStatus == "REJECTED");
                }
                if (status.ToUpper() == "CANCELLED")
                {
                    query = query.Where(x => x.la.ApprovalStatus == "CANCELLED");
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToUpper();
                query = query.Where(x =>
                    (x.e.EmployeeCode ?? "").ToUpper().Contains(search) ||
                    ((x.e.FirstName + " " + (x.e.LastName ?? "")).ToUpper().Contains(search)));
            }

            return await query
                .OrderByDescending(x => x.la.FromDate)
                .ThenBy(x => x.e.LastName)
                .Select(x => new LeaveRequestRowDto
                {
                    IdLeaveApplication = x.la.IdLeaveApplication,
                    IdEmployee = x.e.IdEmployee.Value,
                    EmployeeCode = x.e.EmployeeCode,
                    EmployeeName = (x.e.LastName ?? "") + ", " + x.e.FirstName,
                    Department = x.d.DepartmentName,
                    Designation = x.g.DesignationName,
                    LeaveTypeName = x.la.LeaveTypeName,
                    FromDate = x.la.FromDate,
                    ToDate = x.la.ToDate,
                    TotalLeaveDays = x.la.TotalLeaveDays,
                    ApprovalStatus = x.la.ApprovalStatus,
                    ApplicationStatus = x.la.ApplicationStatus,
                    PhoneNumber1 = x.e.PhoneNumber1,
                    PhoneNumber2 = x.e.PhoneNumber2
                })
                .ToListAsync();
        }


        public async Task<List<LeaveRequestRowDto>> GetTodayStatusDetails(DateTime Date, string QueryType)
        {
            Date = Date.Date;


            var query =
                from la in _dbContext.LeaveApplications
                join e in _dbContext.Employees
                    on la.IdEmployee equals e.IdEmployee
                join d in _dbContext.Departments
                    on e.IdDepartment equals d.IdDepartment
                join g in _dbContext.Designations
                    on e.IdDesignation equals g.IdDesignation
                where la.FromDate <= Date && la.ToDate >= Date
                select new
                {
                    LeaveApplication = la,
                    Employee = e,
                    Department = d,
                    Designation = g
                };

            if (QueryType == "WORKFROMHOME")
            {
                // adjust the property name/condition as per your model
                query = query.Where(x => x.LeaveApplication.LeaveTypeName.ToUpper() == "WORK FROM HOME");
            }
            else
            {
                // adjust the property name/condition as per your model
                query = query.Where(x => x.LeaveApplication.LeaveTypeName.ToUpper() != "WORK FROM HOME");
            }

            return await query
                .OrderByDescending(x => x.Employee.FirstName)
                .Select(x => new LeaveRequestRowDto
                {
                    IdLeaveApplication = x.LeaveApplication.IdLeaveApplication,
                    IdEmployee = x.Employee.IdEmployee.Value,
                    EmployeeCode = x.Employee.EmployeeCode,
                    EmployeeName = (x.Employee.LastName ?? "") + ", " + x.Employee.FirstName,
                    Department = x.Department.DepartmentName,
                    Designation = x.Designation.DesignationName,
                    LeaveTypeName = x.LeaveApplication.LeaveTypeName,
                    FromDate = x.LeaveApplication.FromDate,
                    ToDate = x.LeaveApplication.ToDate,
                    TotalLeaveDays = x.LeaveApplication.TotalLeaveDays,
                    ApprovalStatus = x.LeaveApplication.ApprovalStatus,
                    ApplicationStatus = x.LeaveApplication.ApplicationStatus,
                    PhoneNumber1 = x.Employee.PhoneNumber1,
                    PhoneNumber2 = x.Employee.PhoneNumber2
                })
                .ToListAsync();
        }

        public async Task<List<NotClockedEmployeeWithShiftDto>> GetNotClockedInWithShiftAsync(DateTime date)
        {
            var today = date.Date;

            // 1) Call stored procedure
            var shiftConfig = await _dbContext
                .Set<EmployeeShiftResult>()
                .FromSqlRaw(
                    "EXEC GetShiftConfigurationAllEmployees @Date",
                    new SqlParameter("@Date", today))
                .ToListAsync();

            // 2) All employees who have any clock record today
            var clockedIds = await _dbContext.ClockInOutDetails
                .Where(c => c.ClockDate.Date == today)
                .Select(c => c.IdEmployee)
                .Distinct()
                .ToListAsync();

            var clockedSet = clockedIds.ToHashSet();

            // 3) Preload employee + department + designation
            var employeesQuery =
                from e in _dbContext.Employees
                join d in _dbContext.Departments on e.IdDepartment equals d.IdDepartment
                join g in _dbContext.Designations on e.IdDesignation equals g.IdDesignation
                select new { e, d, g };

            var employees = await employeesQuery.ToListAsync();

            // 4) Join shiftConfig with employees in memory and filter:
            //    working day, not on leave, not clocked
            var result =
                (from s in shiftConfig
                 join emp in employees
                     on s.IdEmployee equals emp.e.IdEmployee into empGroup
                 from emp in empGroup.DefaultIfEmpty()
                 where s.IsWorkingDay == "Y"
                       && s.IsOnLeave != "Y"
                       && !clockedSet.Contains(s.IdEmployee)
                 select new NotClockedEmployeeWithShiftDto
                 {
                     IdEmployee = s.IdEmployee,
                     EmployeeCode = emp?.e.EmployeeCode,
                     EmployeeName = s.EmployeeName,              // from stored proc
                     EmailId = s.EmailId,                   // from stored proc
                     DepartmentName = emp?.d.DepartmentName ?? "", // from master data
                     DesignationName = emp?.g.DesignationName ?? "",
                     ExpectedStartTime = s.StartTime,
                     ExpectedEndTime = s.EndTime,
                     StatusDetails = s.StatusDetails
                 })
                .ToList();

            return result;
        }
        #endregion

    }
}
