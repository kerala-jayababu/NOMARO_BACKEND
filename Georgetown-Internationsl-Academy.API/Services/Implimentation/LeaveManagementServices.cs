using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class LeaveManaementServices : ILeaveManaementServices
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<NotificationConfigService> _logger;

        public LeaveManaementServices(IConfiguration configuration, ApplicationDBContext dbContext, ILogger<NotificationConfigService> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }
        #region LeaveTypes

        public async Task<IEnumerable<LeaveTypesDto>> GetLeaveTypes()
        {
            try
            {
                var leaveTypes = await _dbContext.LeaveTypes
                    .OrderBy(x => x.LeaveTypeName)
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


        public async Task<bool> AddOrUpdateLeaveTypes(List<LeaveTypesDto> leaveTypeDtoList)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ✅ Validation: duplicates in request itself
                var duplicateCodes = leaveTypeDtoList
                    .Where(x => !string.IsNullOrWhiteSpace(x.LeaveCode))
                    .GroupBy(x => x.LeaveCode.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateCodes.Any())
                    throw new ArgumentException($"Duplicate LeaveCode found in request: {string.Join(", ", duplicateCodes)}");

                var duplicateNames = leaveTypeDtoList
                    .Where(x => !string.IsNullOrWhiteSpace(x.LeaveTypeName))
                    .GroupBy(x => x.LeaveTypeName.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateNames.Any())
                    throw new ArgumentException($"Duplicate LeaveTypeName found in request: {string.Join(", ", duplicateNames)}");

                foreach (var dto in leaveTypeDtoList)
                {
                    if (string.IsNullOrWhiteSpace(dto.LeaveCode))
                        throw new ArgumentException("LeaveCode is required.");

                    if (string.IsNullOrWhiteSpace(dto.LeaveTypeName))
                        throw new ArgumentException("LeaveTypeName is required.");

                    // ✅ normalize
                    dto.LeaveCode = dto.LeaveCode.Trim();
                    dto.LeaveTypeName = dto.LeaveTypeName.Trim();

                    // ✅ Update
                    if (dto.IdLeaveType > 0)
                    {
                        var existing = await _dbContext.LeaveTypes
                            .FirstOrDefaultAsync(x => x.IdLeaveType == dto.IdLeaveType);

                        if (existing == null)
                            throw new ArgumentException($"LeaveType not found. IdLeaveType = {dto.IdLeaveType}");

                        // ✅ Uniqueness check in DB (LeaveCode)
                        bool codeExists = await _dbContext.LeaveTypes.AnyAsync(x =>
                            x.LeaveCode == dto.LeaveCode &&
                            x.IdLeaveType != dto.IdLeaveType);

                        if (codeExists)
                            throw new ArgumentException($"LeaveCode '{dto.LeaveCode}' already exists.");

                        // ✅ Uniqueness check in DB (LeaveTypeName)
                        bool nameExists = await _dbContext.LeaveTypes.AnyAsync(x =>
                            x.LeaveTypeName == dto.LeaveTypeName &&
                            x.IdLeaveType != dto.IdLeaveType);

                        if (nameExists)
                            throw new ArgumentException($"LeaveTypeName '{dto.LeaveTypeName}' already exists.");

                        existing.LeaveCode = dto.LeaveCode;
                        existing.LeaveTypeName = dto.LeaveTypeName;

                        // ✅ if your LeaveTypes table has UpdatedAt/UpdatedBy columns
                        // existing.UpdatedAt = DateTime.Now;
                        // existing.UpdatedBy = loggedInEmployeeId;

                        _dbContext.LeaveTypes.Update(existing);
                    }
                    else
                    {
                        // ✅ Insert uniqueness check
                        bool codeExists = await _dbContext.LeaveTypes.AnyAsync(x => x.LeaveCode == dto.LeaveCode);
                        if (codeExists)
                            throw new ArgumentException($"LeaveCode '{dto.LeaveCode}' already exists.");

                        bool nameExists = await _dbContext.LeaveTypes.AnyAsync(x => x.LeaveTypeName == dto.LeaveTypeName);
                        if (nameExists)
                            throw new ArgumentException($"LeaveTypeName '{dto.LeaveTypeName}' already exists.");

                        var newLeaveType = new LeaveTypes
                        {
                            LeaveCode = dto.LeaveCode,
                            LeaveTypeName = dto.LeaveTypeName,
                        };

                        // ✅ if your LeaveTypes table has CreatedAt/CreatedBy columns
                        // newLeaveType.CreatedAt = DateTime.Now;
                        // newLeaveType.CreatedBy = loggedInEmployeeId;

                        await _dbContext.LeaveTypes.AddAsync(newLeaveType);
                    }
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

        #region AnnualLeaveTypeConfig

        public async Task<IEnumerable<AnnualLeaveTypeConfigDto>> GetAnnualLeaveTypeConfigs(
         int? idAnnualLeaveTypeConfig = null,
         int? idLeaveType = null,
         int? idYear = null,
         bool? isActive = null)
        {
            try
            {
                var query = _dbContext.AnnualLeaveTypeConfig.AsQueryable();

                if (idAnnualLeaveTypeConfig.HasValue)
                    query = query.Where(x => x.IdAnnualLeaveTypeConfig == idAnnualLeaveTypeConfig.Value);

                if (idLeaveType.HasValue)
                    query = query.Where(x => x.IdLeaveType == idLeaveType.Value);

                if (idYear.HasValue)
                    query = query.Where(x => x.IdYear == idYear.Value);

                if (isActive.HasValue)
                    query = query.Where(x => x.IsActive == isActive.Value);

                var result = await query
                    .OrderBy(x => x.LeaveTypeName)
                    .Select(x => new AnnualLeaveTypeConfigDto
                    {
                        IdAnnualLeaveTypeConfig = x.IdAnnualLeaveTypeConfig,
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        LeaveCode = x.LeaveCode,
                        IdYear = x.IdYear,
                        EffectiveFrom = x.EffectiveFrom,
                        EffectiveTo = x.EffectiveTo,
                        IsPaid = x.IsPaid,
                        SalaryDeductionPercent = x.SalaryDeductionPercent,
                        AllowHalfDay = x.AllowHalfDay,
                        RequiresApproval = x.RequiresApproval,
                        RequiredApprovalLevel = x.RequiredApprovalLevel,
                        RequiresDocument = x.RequiresDocument,
                        DocumentRequiredAfterDays = x.DocumentRequiredAfterDays,
                        IsCarryForwardAllowed = x.IsCarryForwardAllowed,
                        MaxCarryForwardDays = x.MaxCarryForwardDays,
                        ApplicableGender = x.ApplicableGender,
                        IsActive = x.IsActive,
                        IncludeHolidaysBetween = x.IncludeHolidaysBetween,
                        MaxLeavesPerYear = x.MaxLeavesPerYear,
                        MaxLeavesPerMonth = x.MaxLeavesPerMonth,
                        AllowBackdatedLeave = x.AllowBackdatedLeave,
                        BackdateLimitDays = x.BackdateLimitDays,
                        CreatedBy = x.CreatedBy,
                        CreatedAt = x.CreatedAt,
                        UpdatedBy = x.UpdatedBy,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching AnnualLeaveTypeConfigs.");
                throw;
            }
        }


        public async Task<List<int>> AddUpdateAnnualLeaveTypeConfig(
     List<AnnualLeaveTypeConfigDto> configDtoList,
     int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var savedIds = new List<int>();

                foreach (var dto in configDtoList)
                {
                    // ✅ VALIDATIONS
                    if (dto.SalaryDeductionPercent < 0 || dto.SalaryDeductionPercent > 100)
                        throw new ArgumentException("SalaryDeductionPercent must be between 0 and 100.");

                    if (dto.EffectiveFrom > dto.EffectiveTo)
                        throw new ArgumentException("EffectiveFrom cannot be greater than EffectiveTo.");

                    if (dto.RequiresDocument && (!dto.DocumentRequiredAfterDays.HasValue || dto.DocumentRequiredAfterDays.Value < 0))
                        throw new ArgumentException("DocumentRequiredAfterDays must be >= 0 when RequiresDocument is enabled.");

                    if (dto.IsPaid && dto.SalaryDeductionPercent != 0)
                        throw new ArgumentException("SalaryDeductionPercent must be 0 when IsPaid is true.");

                    // ✅ One Active Config per (IdLeaveType, IdYear)
                    if (dto.IsActive)
                    {
                        bool activeExists = await _dbContext.AnnualLeaveTypeConfig.AnyAsync(x =>
                            x.IdLeaveType == dto.IdLeaveType &&
                            x.IdYear == dto.IdYear &&
                            x.IsActive == true &&
                            x.IdAnnualLeaveTypeConfig != dto.IdAnnualLeaveTypeConfig
                        );

                        if (activeExists)
                            throw new ArgumentException("Only one active config is allowed for the selected LeaveType and Year.");
                    }

                    // ✅ Update
                    if (dto.IdAnnualLeaveTypeConfig > 0)
                    {
                        var existing = await _dbContext.AnnualLeaveTypeConfig
                            .FirstOrDefaultAsync(x => x.IdAnnualLeaveTypeConfig == dto.IdAnnualLeaveTypeConfig);

                        if (existing == null)
                            throw new ArgumentException($"Config not found. IdAnnualLeaveTypeConfig = {dto.IdAnnualLeaveTypeConfig}");

                        existing.IdLeaveType = dto.IdLeaveType;
                        existing.LeaveTypeName = dto.LeaveTypeName;
                        existing.LeaveCode = dto.LeaveCode;
                        existing.IdYear = dto.IdYear;

                        existing.EffectiveFrom = dto.EffectiveFrom;
                        existing.EffectiveTo = dto.EffectiveTo;

                        existing.IsPaid = dto.IsPaid;
                        existing.SalaryDeductionPercent = dto.SalaryDeductionPercent;

                        existing.AllowHalfDay = dto.AllowHalfDay;
                        existing.RequiresApproval = dto.RequiresApproval;
                        existing.RequiredApprovalLevel = dto.RequiredApprovalLevel;

                        existing.RequiresDocument = dto.RequiresDocument;
                        existing.DocumentRequiredAfterDays = dto.DocumentRequiredAfterDays;

                        existing.IsCarryForwardAllowed = dto.IsCarryForwardAllowed;
                        existing.MaxCarryForwardDays = dto.MaxCarryForwardDays;

                        existing.ApplicableGender = dto.ApplicableGender;
                        existing.IsActive = dto.IsActive;

                        existing.IncludeHolidaysBetween = dto.IncludeHolidaysBetween;

                        existing.MaxLeavesPerYear = dto.MaxLeavesPerYear;
                        existing.MaxLeavesPerMonth = dto.MaxLeavesPerMonth;

                        existing.AllowBackdatedLeave = dto.AllowBackdatedLeave;
                        existing.BackdateLimitDays = dto.BackdateLimitDays;

                        existing.UpdatedBy = loggedInEmployeeId;
                        existing.UpdatedAt = DateTime.Now;

                        _dbContext.AnnualLeaveTypeConfig.Update(existing);

                        savedIds.Add(existing.IdAnnualLeaveTypeConfig);
                    }
                    else
                    {
                        // ✅ Insert new
                        var newConfig = new AnnualLeaveTypeConfig
                        {
                            IdLeaveType = dto.IdLeaveType,
                            LeaveTypeName = dto.LeaveTypeName,
                            LeaveCode = dto.LeaveCode,
                            IdYear = dto.IdYear,

                            EffectiveFrom = dto.EffectiveFrom,
                            EffectiveTo = dto.EffectiveTo,

                            IsPaid = dto.IsPaid,
                            SalaryDeductionPercent = dto.SalaryDeductionPercent,

                            AllowHalfDay = dto.AllowHalfDay,
                            RequiresApproval = dto.RequiresApproval,
                            RequiredApprovalLevel = dto.RequiredApprovalLevel,

                            RequiresDocument = dto.RequiresDocument,
                            DocumentRequiredAfterDays = dto.DocumentRequiredAfterDays,

                            IsCarryForwardAllowed = dto.IsCarryForwardAllowed,
                            MaxCarryForwardDays = dto.MaxCarryForwardDays,

                            ApplicableGender = dto.ApplicableGender,
                            IsActive = dto.IsActive,

                            IncludeHolidaysBetween = dto.IncludeHolidaysBetween,

                            MaxLeavesPerYear = dto.MaxLeavesPerYear,
                            MaxLeavesPerMonth = dto.MaxLeavesPerMonth,

                            AllowBackdatedLeave = dto.AllowBackdatedLeave,
                            BackdateLimitDays = dto.BackdateLimitDays,

                            CreatedBy = loggedInEmployeeId,
                            CreatedAt = DateTime.Now
                        };

                        await _dbContext.AnnualLeaveTypeConfig.AddAsync(newConfig);

                        // SaveChanges required to generate ID
                        await _dbContext.SaveChangesAsync();

                        savedIds.Add(newConfig.IdAnnualLeaveTypeConfig);
                    }
                }

                await transaction.CommitAsync();
                return savedIds;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating AnnualLeaveTypeConfig.");
                throw;
            }
        }
        public async Task<bool> DeactivateAnnualLeaveTypeConfig(int idAnnualLeaveTypeConfig, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var config = await _dbContext.AnnualLeaveTypeConfig
                    .FirstOrDefaultAsync(x => x.IdAnnualLeaveTypeConfig == idAnnualLeaveTypeConfig);

                if (config == null)
                    throw new ArgumentException("Annual leave configuration record not found.");

                if (!config.IsActive)
                    throw new ArgumentException("Annual leave configuration is already inactive.");

                // ✅ Block if referenced by active templates / assignments
                // ⚠️ Replace these checks with your actual tables

                bool isUsedInActiveTemplates = false; // Example: await _dbContext.LeaveTemplates.AnyAsync(...)
                bool isUsedInEmployeeAssignments = false; // Example: await _dbContext.EmployeeLeaveAssignments.AnyAsync(...)

                if (isUsedInActiveTemplates || isUsedInEmployeeAssignments)
                {
                    // ✅ If referenced, allow only end-date (EffectiveTo) and require replacement exists
                    // Placeholder replacement logic
                    bool replacementExists = await _dbContext.AnnualLeaveTypeConfig.AnyAsync(x =>
                        x.IdLeaveType == config.IdLeaveType &&
                        x.IdYear == config.IdYear &&
                        x.IsActive == true &&
                        x.IdAnnualLeaveTypeConfig != config.IdAnnualLeaveTypeConfig
                    );

                    if (!replacementExists)
                    {
                        throw new ArgumentException(
                            "Cannot deactivate this config because it is referenced by active templates/assignments and no active replacement exists.");
                    }

                    // ✅ End-date instead of deactivating immediately
                    config.EffectiveTo = DateTime.Today;
                    config.IsActive = false;
                }
                else
                {
                    // ✅ Normal soft delete
                    config.IsActive = false;

                    // Optional: End-date it to today
                    if (config.EffectiveTo > DateTime.Today)
                        config.EffectiveTo = DateTime.Today;
                }

                config.UpdatedBy = loggedInEmployeeId;
                config.UpdatedAt = DateTime.Now;

                _dbContext.AnnualLeaveTypeConfig.Update(config);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deactivating annual leave config. IdAnnualLeaveTypeConfig: {Id}", idAnnualLeaveTypeConfig);
                throw;
            }
        }


        #endregion

        #region LeaveTemplates
        public async Task<IEnumerable<LeaveTemplateDto>> GetLeaveTemplates(
     int? idLeaveTemplate = null,
     bool? isActive = null,
     string? searchText = null)
        {
            try
            {
                var query = _dbContext.LeaveTemplates.AsQueryable();

                if (idLeaveTemplate.HasValue)
                    query = query.Where(x => x.IdLeaveTemplate == idLeaveTemplate.Value);

                if (isActive.HasValue)
                    query = query.Where(x => x.IsActive == isActive.Value);

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
                        IdAnnualLeaveTypeConfig = t.IdAnnualLeaveTypeConfig,  // ✅ optional, keep if you want

                        IsActive = t.IsActive,
                        CreatedBy = t.CreatedBy,
                        CreatedAt = t.CreatedAt,
                        UpdatedBy = t.UpdatedBy,
                        UpdatedAt = t.UpdatedAt
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

        public async Task<LeaveTemplateWithDetailsDto> GetLeaveTemplate(int idLeaveTemplate)
        {
            try
            {
                var header = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == idLeaveTemplate);

                if (header == null)
                    throw new ArgumentException("Leave template not found.");

                // ✅ Load Header Config (if present)
                AnnualLeaveTypeConfigDto? headerConfig = null;

                if (header.IdAnnualLeaveTypeConfig.HasValue && header.IdAnnualLeaveTypeConfig.Value > 0)
                {
                    headerConfig = await _dbContext.AnnualLeaveTypeConfig
                        .Where(c => c.IdAnnualLeaveTypeConfig == header.IdAnnualLeaveTypeConfig.Value)
                        .Select(c => new AnnualLeaveTypeConfigDto
                        {
                            IdAnnualLeaveTypeConfig = c.IdAnnualLeaveTypeConfig,
                            IdLeaveType = c.IdLeaveType,
                            LeaveTypeName = c.LeaveTypeName,
                            LeaveCode = c.LeaveCode,
                            IdYear = c.IdYear,
                            EffectiveFrom = c.EffectiveFrom,
                            EffectiveTo = c.EffectiveTo,
                            IsPaid = c.IsPaid,
                            SalaryDeductionPercent = c.SalaryDeductionPercent,
                            AllowHalfDay = c.AllowHalfDay,
                            RequiresApproval = c.RequiresApproval,
                            RequiredApprovalLevel = c.RequiredApprovalLevel,
                            RequiresDocument = c.RequiresDocument,
                            DocumentRequiredAfterDays = c.DocumentRequiredAfterDays,
                            IsCarryForwardAllowed = c.IsCarryForwardAllowed,
                            MaxCarryForwardDays = c.MaxCarryForwardDays,
                            ApplicableGender = c.ApplicableGender,
                            IsActive = c.IsActive,
                            IncludeHolidaysBetween = c.IncludeHolidaysBetween,
                            MaxLeavesPerYear = c.MaxLeavesPerYear,
                            MaxLeavesPerMonth = c.MaxLeavesPerMonth,
                            AllowBackdatedLeave = c.AllowBackdatedLeave,
                            BackdateLimitDays = c.BackdateLimitDays,
                            CreatedBy = c.CreatedBy,
                            CreatedAt = c.CreatedAt,
                            UpdatedBy = c.UpdatedBy,
                            UpdatedAt = c.UpdatedAt
                        })
                        .FirstOrDefaultAsync();
                }

                // ✅ Load details
                var details = await _dbContext.LeaveTemplateDetails
                    .Where(x => x.IdLeaveTemplate == idLeaveTemplate)
                    .OrderBy(x => x.IdLeaveTemplateDetails)
                    .Select(x => new LeaveTemplateDetailsDto
                    {
                        IdLeaveTemplateDetails = x.IdLeaveTemplateDetails,
                        IdLeaveTemplate = x.IdLeaveTemplate,
                        IdLeaveType = x.IdLeaveType,
                        IdAnnualLeaveTypeConfig = x.IdAnnualLeaveTypeConfig,
                        NoOfDaysInYear = x.NoOfDaysInYear
                    })
                    .ToListAsync();

                // ✅ Fetch configs for details only once
                var configIds = details
                    .Where(d => d.IdAnnualLeaveTypeConfig.HasValue && d.IdAnnualLeaveTypeConfig.Value > 0)
                    .Select(d => d.IdAnnualLeaveTypeConfig.Value)
                    .Distinct()
                    .ToList();

                var configs = await _dbContext.AnnualLeaveTypeConfig
                    .Where(c => configIds.Contains(c.IdAnnualLeaveTypeConfig))
                    .Select(c => new AnnualLeaveTypeConfigDto
                    {
                        IdAnnualLeaveTypeConfig = c.IdAnnualLeaveTypeConfig,
                        IdLeaveType = c.IdLeaveType,
                        LeaveTypeName = c.LeaveTypeName,
                        LeaveCode = c.LeaveCode,
                        IdYear = c.IdYear,
                        EffectiveFrom = c.EffectiveFrom,
                        EffectiveTo = c.EffectiveTo,
                        IsPaid = c.IsPaid,
                        SalaryDeductionPercent = c.SalaryDeductionPercent,
                        AllowHalfDay = c.AllowHalfDay,
                        RequiresApproval = c.RequiresApproval,
                        RequiredApprovalLevel = c.RequiredApprovalLevel,
                        RequiresDocument = c.RequiresDocument,
                        DocumentRequiredAfterDays = c.DocumentRequiredAfterDays,
                        IsCarryForwardAllowed = c.IsCarryForwardAllowed,
                        MaxCarryForwardDays = c.MaxCarryForwardDays,
                        ApplicableGender = c.ApplicableGender,
                        IsActive = c.IsActive,
                        IncludeHolidaysBetween = c.IncludeHolidaysBetween,
                        MaxLeavesPerYear = c.MaxLeavesPerYear,
                        MaxLeavesPerMonth = c.MaxLeavesPerMonth,
                        AllowBackdatedLeave = c.AllowBackdatedLeave,
                        BackdateLimitDays = c.BackdateLimitDays,
                        CreatedBy = c.CreatedBy,
                        CreatedAt = c.CreatedAt,
                        UpdatedBy = c.UpdatedBy,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                // ✅ Attach config object to each detail
                foreach (var d in details)
                {
                    if (d.IdAnnualLeaveTypeConfig.HasValue && d.IdAnnualLeaveTypeConfig.Value > 0)
                    {
                        d.AnnualLeaveTypeConfig = configs
                            .FirstOrDefault(c => c.IdAnnualLeaveTypeConfig == d.IdAnnualLeaveTypeConfig.Value);
                    }
                }

                return new LeaveTemplateWithDetailsDto
                {
                    IdLeaveTemplate = header.IdLeaveTemplate,
                    LeaveTemplateName = header.LeaveTemplateName,
                    LeaveTemplateDesc = header.LeaveTemplateDesc,
                    IdAnnualLeaveTypeConfig = header.IdAnnualLeaveTypeConfig,

                    AnnualLeaveTypeConfig = headerConfig,   // ✅ Header Config object

                    IsActive = header.IsActive,
                    CreatedBy = header.CreatedBy,
                    CreatedAt = header.CreatedAt,
                    UpdatedBy = header.UpdatedBy,
                    UpdatedAt = header.UpdatedAt,

                    Details = details
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching LeaveTemplate. IdLeaveTemplate: {Id}", idLeaveTemplate);
                throw;
            }
        }

        public async Task<LeaveTemplateSaveResponseDto> AddUpdateLeaveTemplate(LeaveTemplatePostDto dto, int loggedInEmployeeId)
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

                // ✅ (2) If IsActive = true -> at least 1 detail row required
                if (dto.IsActive && (dto.Details == null || !dto.Details.Any()))
                    throw new ArgumentException("At least one detail row is required when template is active.");

                // ✅ (3) No duplicate LeaveType within template
                if (dto.Details != null && dto.Details.Any())
                {
                    var duplicateLeaveTypes = dto.Details
                        .GroupBy(x => x.IdLeaveType)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateLeaveTypes.Any())
                        throw new ArgumentException($"Duplicate LeaveType found in template: {string.Join(", ", duplicateLeaveTypes)}");
                }

                // ✅ (4) Insert/Update Header
                LeaveTemplates headerEntity;

                if (dto.IdLeaveTemplate > 0)
                {
                    headerEntity = await _dbContext.LeaveTemplates
                        .FirstOrDefaultAsync(x => x.IdLeaveTemplate == dto.IdLeaveTemplate);

                    if (headerEntity == null)
                        throw new ArgumentException("Leave template not found.");

                    headerEntity.LeaveTemplateName = dto.LeaveTemplateName;
                    headerEntity.LeaveTemplateDesc = dto.LeaveTemplateDesc;
                    headerEntity.IsActive = dto.IsActive;

                    // If you have this column in DB:
                    if (dto.IdAnnualLeaveTypeConfig.HasValue)
                    {
                        headerEntity.IdAnnualLeaveTypeConfig = dto.IdAnnualLeaveTypeConfig.Value;
                    }

                    headerEntity.UpdatedAt = DateTime.Now;
                    headerEntity.UpdatedBy = loggedInEmployeeId;

                    _dbContext.LeaveTemplates.Update(headerEntity);
                }
                else
                {
                    headerEntity = new LeaveTemplates
                    {
                        LeaveTemplateName = dto.LeaveTemplateName,
                        LeaveTemplateDesc = dto.LeaveTemplateDesc,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now,
                        CreatedBy = loggedInEmployeeId
                    };

                    if (dto.IdAnnualLeaveTypeConfig.HasValue)
                    {
                        headerEntity.IdAnnualLeaveTypeConfig = dto.IdAnnualLeaveTypeConfig.Value;
                    }

                    await _dbContext.LeaveTemplates.AddAsync(headerEntity);
                    await _dbContext.SaveChangesAsync(); // ✅ generate header ID
                }

                int templateId = headerEntity.IdLeaveTemplate;

                // ✅ (5) Upsert Details
                var existingDetails = await _dbContext.LeaveTemplateDetails
                    .Where(x => x.IdLeaveTemplate == templateId)
                    .ToListAsync();

                // ✅ OPTIONAL RESTRICTION: If template is assigned, block delete removed leave types
                bool isTemplateAssigned = false;

                // ✅ Example: Replace with your actual assignment table
                //isTemplateAssigned = await _dbContext.EmployeeLeaveTemplateAssignments
                //     .AnyAsync(x => x.IdLeaveTemplate == templateId && x.IsActive == true);

                // Keep list of detail IDs coming in request
                var detailIdsInRequest = dto.Details
                    .Where(x => x.IdLeaveTemplateDetails > 0)
                    .Select(x => x.IdLeaveTemplateDetails)
                    .ToList();

                // ✅ Delete removed details (if allowed)
                var detailsToDelete = existingDetails
                    .Where(x => !detailIdsInRequest.Contains(x.IdLeaveTemplateDetails))
                    .ToList();

                if (detailsToDelete.Any())
                {
                    if (isTemplateAssigned)
                    {
                        throw new ArgumentException("Cannot remove leave types because this template is assigned to employees. End-date or migrate instead.");
                    }

                    _dbContext.LeaveTemplateDetails.RemoveRange(detailsToDelete);
                }

                // ✅ Insert or Update details
                foreach (var detailDto in dto.Details)
                {
                    if (detailDto.NoOfDaysInYear <= 0)
                        throw new ArgumentException("NoOfDaysInYear must be greater than 0.");

                    var existing = existingDetails
                        .FirstOrDefault(x => x.IdLeaveTemplateDetails == detailDto.IdLeaveTemplateDetails);

                    if (existing != null)
                    {
                        existing.IdLeaveType = detailDto.IdLeaveType;

                        // if column exists:
                        if (detailDto.IdAnnualLeaveTypeConfig.HasValue)
                            existing.IdAnnualLeaveTypeConfig = detailDto.IdAnnualLeaveTypeConfig.Value;

                        existing.NoOfDaysInYear = detailDto.NoOfDaysInYear;

                        _dbContext.LeaveTemplateDetails.Update(existing);
                    }
                    else
                    {
                        var newDetail = new LeaveTemplateDetails
                        {
                            IdLeaveTemplate = templateId,
                            IdLeaveType = detailDto.IdLeaveType,
                            NoOfDaysInYear = detailDto.NoOfDaysInYear
                        };

                        if (detailDto.IdAnnualLeaveTypeConfig.HasValue)
                            newDetail.IdAnnualLeaveTypeConfig = detailDto.IdAnnualLeaveTypeConfig.Value;

                        await _dbContext.LeaveTemplateDetails.AddAsync(newDetail);
                    }
                }

                await _dbContext.SaveChangesAsync();

                // ✅ (6) Audit log (optional)
                // await _auditService.Log(...)

                await transaction.CommitAsync();

                // ✅ Return full template after save
                var savedTemplate = await GetLeaveTemplate(templateId);

                return new LeaveTemplateSaveResponseDto
                {
                    SuccessFlag = true,
                    Message = "Leave template saved successfully.",
                    IdLeaveTemplate = templateId,
                    Template = savedTemplate
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving Leave Template.");
                throw;
            }
        }
        public async Task<bool> DeactivateLeaveTemplate(int idLeaveTemplate, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var template = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(x => x.IdLeaveTemplate == idLeaveTemplate);

                if (template == null)
                    throw new ArgumentException("Leave template not found.");

                if (!template.IsActive)
                    throw new ArgumentException("Leave template is already inactive.");

                // ✅ Block if template has active employee assignments
                // ⚠️ Replace this with your actual assignment table check
                bool hasActiveAssignments = false;

                // Example:
                // hasActiveAssignments = await _dbContext.EmployeeLeaveTemplateAssignments.AnyAsync(x =>
                //     x.IdLeaveTemplate == idLeaveTemplate && x.IsActive == true);

                if (hasActiveAssignments)
                    throw new ArgumentException("Cannot deactivate template because it has active employee assignments. End-date assignments first.");

                // ✅ Soft delete
                template.IsActive = false;
                template.UpdatedAt = DateTime.Now;
                template.UpdatedBy = loggedInEmployeeId;

                _dbContext.LeaveTemplates.Update(template);
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

        #region EmployeeLeaveManagement
        public async Task<EmployeeLeaveSetupDto> GetEmployeeLeaveSetup(int idEmployee, DateTime? activeOnDate = null)
        {
            try
            {
                if (idEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                var date = (activeOnDate ?? DateTime.Now).Date;

                // ✅ Find active config
                var config = await _dbContext.EmployeeLeaveConfigs
                    .Where(x => x.IdEmployee == idEmployee
                             && x.EffectiveFrom.Date <= date
                             && (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= date))
                    .OrderByDescending(x => x.EffectiveFrom) // latest active assignment
                    .FirstOrDefaultAsync();

                if (config == null)
                    throw new ArgumentException("No active leave setup found for this employee.");

                // ✅ Load Details
                var details = await (
                    from d in _dbContext.EmployeeLeaveConfigDetails
                    where d.IdEmployeeLeaveConfig == config.IdEmployeeLeaveConfig
                    join lt in _dbContext.LeaveTypes on d.IdLeaveType equals lt.IdLeaveType into lts
                    from leaveType in lts.DefaultIfEmpty()
                    orderby d.IdLeaveType
                    select new EmployeeLeaveSetupDetailDto
                    {
                        IdEmployeeLeaveConfigDetail = d.IdEmployeeLeaveConfigDetail,
                        IdEmployeeLeaveConfig = d.IdEmployeeLeaveConfig,
                        IdLeaveType = d.IdLeaveType,
                        AllocatedDays = d.AllocatedDays,
                        UsedLeaveDays = d.UsedLeaveDays,
                        BalanceLeaveDays = d.BalanceLeaveDays,

                        LeaveTypeName = leaveType != null ? leaveType.LeaveTypeName : null,
                        LeaveCode = leaveType != null ? leaveType.LeaveCode : null
                    }
                ).ToListAsync();

                return new EmployeeLeaveSetupDto
                {
                    IdEmployeeLeaveConfig = config.IdEmployeeLeaveConfig,
                    IdEmployee = config.IdEmployee,
                    IdLeaveTemplate = config.IdLeaveTemplate,
                    EffectiveFrom = config.EffectiveFrom,
                    EffectiveTo = config.EffectiveTo,
                    CreatedBy = config.CreatedBy,
                    CreatedAt = config.CreatedAt,
                    UpdatedBy = config.UpdatedBy,
                    UpdatedAt = config.UpdatedAt,
                    Details = details
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee leave setup. IdEmployee={IdEmployee}", idEmployee);
                throw;
            }
        }
        public async Task<int> AddUpdateEmployeeLeaveConfig(EmployeeLeaveConfigPostDto dto, int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ✅ Basic validations
                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                if (dto.IdLeaveTemplate <= 0)
                    throw new ArgumentException("IdLeaveTemplate is required.");

                if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
                    throw new ArgumentException("EffectiveTo cannot be earlier than EffectiveFrom.");

                // ✅ (1) Validate employee exists
                bool employeeExists = await _dbContext.Employees.AnyAsync(e => e.IdEmployee == dto.IdEmployee);
                if (!employeeExists)
                    throw new ArgumentException("Employee not found.");

                // ✅ (2) Validate template exists and is active
                var template = await _dbContext.LeaveTemplates
                    .FirstOrDefaultAsync(t => t.IdLeaveTemplate == dto.IdLeaveTemplate);

                if (template == null)
                    throw new ArgumentException("Leave template not found.");

                if (!template.IsActive)
                    throw new ArgumentException("Leave template is not active.");

                // ✅ (3) Validate NO overlapping assignments for employee
                DateTime newFrom = dto.EffectiveFrom.Date;
                DateTime newTo = dto.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

                bool isOverlapping = await _dbContext.EmployeeLeaveConfigs.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee
                    && x.IdEmployeeLeaveConfig != dto.IdEmployeeLeaveConfig
                    && x.EffectiveFrom.Date <= newTo
                    && (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= newFrom));

                if (isOverlapping)
                    throw new ArgumentException("Overlapping leave template assignment exists for this employee.");

                EmployeeLeaveConfigs configEntity;

                // ✅ (4) Insert / Update header
                if (dto.IdEmployeeLeaveConfig > 0)
                {
                    configEntity = await _dbContext.EmployeeLeaveConfigs
                        .FirstOrDefaultAsync(x => x.IdEmployeeLeaveConfig == dto.IdEmployeeLeaveConfig);

                    if (configEntity == null)
                        throw new ArgumentException("Employee leave config not found.");

                    configEntity.IdLeaveTemplate = dto.IdLeaveTemplate;
                    configEntity.EffectiveFrom = dto.EffectiveFrom;
                    configEntity.EffectiveTo = dto.EffectiveTo;
                    configEntity.UpdatedAt = DateTime.Now;
                    configEntity.UpdatedBy = loggedInEmployeeId;

                    _dbContext.EmployeeLeaveConfigs.Update(configEntity);

                    // ✅ Default Rule: Reset balances on update
                    var oldDetails = await _dbContext.EmployeeLeaveConfigDetails
                        .Where(d => d.IdEmployeeLeaveConfig == configEntity.IdEmployeeLeaveConfig)
                        .ToListAsync();

                    _dbContext.EmployeeLeaveConfigDetails.RemoveRange(oldDetails);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    configEntity = new EmployeeLeaveConfigs
                    {
                        IdEmployee = dto.IdEmployee,
                        IdLeaveTemplate = dto.IdLeaveTemplate,
                        EffectiveFrom = dto.EffectiveFrom,
                        EffectiveTo = dto.EffectiveTo,
                        CreatedAt = DateTime.Now,
                        CreatedBy = loggedInEmployeeId
                    };

                    await _dbContext.EmployeeLeaveConfigs.AddAsync(configEntity);
                    await _dbContext.SaveChangesAsync(); // ✅ Generate ID
                }

                int configId = configEntity.IdEmployeeLeaveConfig;

                // ✅ (5) Initialize details from template details
                var templateDetails = await _dbContext.LeaveTemplateDetails
                    .Where(d => d.IdLeaveTemplate == dto.IdLeaveTemplate)
                    .ToListAsync();

                if (templateDetails == null || !templateDetails.Any())
                    throw new ArgumentException("Leave template does not contain any leave types.");

                var newDetails = templateDetails.Select(td => new EmployeeLeaveConfigDetails
                {
                    IdEmployeeLeaveConfig = configId,
                    IdLeaveType = td.IdLeaveType,
                    AllocatedDays = td.NoOfDaysInYear,
                    UsedLeaveDays = 0,
                    BalanceLeaveDays = td.NoOfDaysInYear
                }).ToList();

                await _dbContext.EmployeeLeaveConfigDetails.AddRangeAsync(newDetails);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return configId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding/updating EmployeeLeaveConfig.");
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

                var query = _dbContext.LeaveApplications.AsQueryable();

                if (idEmployee.HasValue && idEmployee.Value > 0)
                    query = query.Where(x => x.IdEmployee == idEmployee.Value);

                if (idLeaveType.HasValue && idLeaveType.Value > 0)
                    query = query.Where(x => x.IdLeaveType == idLeaveType.Value);

                if (!string.IsNullOrWhiteSpace(approvalStatus))
                    query = query.Where(x => x.ApprovalStatus == approvalStatus.Trim());

                if (!string.IsNullOrWhiteSpace(applicationStatus))
                    query = query.Where(x => x.ApplicationStatus == applicationStatus.Trim());

                if (fromDate.HasValue)
                    query = query.Where(x => x.FromDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(x => x.ToDate.Date <= toDate.Value.Date);

                int totalRecords = await query.CountAsync();

                // ✅ Sorting
                bool isDesc = paging.SortOrder?.ToUpper() != "ASC";

                query = paging.SortBy?.ToUpper() switch
                {
                    "FROMDATE" => isDesc ? query.OrderByDescending(x => x.FromDate) : query.OrderBy(x => x.FromDate),
                    "TODATE" => isDesc ? query.OrderByDescending(x => x.ToDate) : query.OrderBy(x => x.ToDate),
                    _ => isDesc ? query.OrderByDescending(x => x.AppliedOn) : query.OrderBy(x => x.AppliedOn)
                };

                // ✅ Paging
                int skip = (paging.PageNumber - 1) * paging.PageSize;

                var data = await query
                    .Skip(skip)
                    .Take(paging.PageSize)
                    .Select(x => new LeaveApplicationListDto
                    {
                        IdLeaveApplication = x.IdLeaveApplication,
                        IdEmployee = x.IdEmployee,
                        IdLeaveType = x.IdLeaveType,
                        LeaveTypeName = x.LeaveTypeName,
                        FromDate = x.FromDate,
                        ToDate = x.ToDate,
                        TotalLeaveDays = x.TotalLeaveDays,
                        ApprovalStatus = x.ApprovalStatus,
                        ApplicationStatus = x.ApplicationStatus,
                        AppliedOn = x.AppliedOn
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

                application.Documents = documents;
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

            var fileName = Path.GetFileName(storedFilePath);

            // GUID_originalname.ext
            var underscoreIndex = fileName.IndexOf('_');

            if (underscoreIndex > 0)
            {
                var prefix = fileName.Substring(0, underscoreIndex);

                if (Guid.TryParse(prefix, out _))
                {
                    return fileName.Substring(underscoreIndex + 1);
                }
            }

            return fileName;
        }
        public async Task<LeaveApplicationSaveResultDto> AddUpdateLeaveApplication(
    LeaveApplicationPostDto dto,
    int loggedInEmployeeId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ✅ Basic validations
                if (dto.IdEmployee <= 0)
                    throw new ArgumentException("IdEmployee is required.");

                if (dto.IdLeaveType <= 0)
                    throw new ArgumentException("IdLeaveType is required.");

                if (string.IsNullOrWhiteSpace(dto.Reason))
                    throw new ArgumentException("Reason is required.");

                if (dto.ToDate.Date < dto.FromDate.Date)
                    throw new ArgumentException("ToDate cannot be earlier than FromDate.");
                // ✅ Leave cannot span across years
                if (dto.FromDate.Year != dto.ToDate.Year)
                    throw new ArgumentException("Leave FromDate and ToDate must be within the same year.");

                // ✅ Employee exists
                bool empExists = await _dbContext.Employees.AnyAsync(e => e.IdEmployee == dto.IdEmployee);
                if (!empExists)
                    throw new ArgumentException("Employee not found.");

                var today = DateTime.Now.Date;

                // ✅ (1) Confirm employee has active template assignment on FromDate
                var activeConfig = await _dbContext.EmployeeLeaveConfigs
                    .Where(x => x.IdEmployee == dto.IdEmployee
                             && x.EffectiveFrom.Date <= dto.FromDate.Date
                             && (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= dto.FromDate.Date))
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();

                if (activeConfig == null)
                    throw new ArgumentException("Employee does not have an active leave setup.");

                // ✅ Get employee balance row for this leave type
                var balanceRow = await _dbContext.EmployeeLeaveConfigDetails
                    .FirstOrDefaultAsync(x =>
                        x.IdEmployeeLeaveConfig == activeConfig.IdEmployeeLeaveConfig
                        && x.IdLeaveType == dto.IdLeaveType);

                if (balanceRow == null)
                    throw new ArgumentException("Leave type is not configured for this employee.");

                // ✅ (2) Fetch annual leave policy (AnnualLeaveTypeConfig) for the leave type + year
                int year = dto.FromDate.Year;

                var annualPolicy = await _dbContext.AnnualLeaveTypeConfig
                    .Where(x => x.IdLeaveType == dto.IdLeaveType
                             && x.IdYear == year
                             && x.IsActive == true)
                    .FirstOrDefaultAsync();

                if (annualPolicy == null)
                    throw new ArgumentException("Annual leave policy not configured for this LeaveType and Year.");

                // ✅ (3) Validate date range within policy effective window
                if (dto.FromDate.Date < annualPolicy.EffectiveFrom.Date || dto.ToDate.Date > annualPolicy.EffectiveTo.Date)
                    throw new ArgumentException("Leave dates are outside the configured policy effective period.");

                // ✅ (4) Half-day rules
                if (dto.IsHalfDay)
                {
                    if (dto.FromDate.Date != dto.ToDate.Date)
                        throw new ArgumentException("Half day leave must be for a single day only.");

                    if (!annualPolicy.AllowHalfDay)
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

                // ✅ (5) Check overlap with existing Pending/Approved leaves
                bool hasOverlap = await _dbContext.LeaveApplications.AnyAsync(x =>
                    x.IdEmployee == dto.IdEmployee
                    && x.IdLeaveApplication != dto.IdLeaveApplication
                    && (x.ApplicationStatus == "Submitted" || x.ApplicationStatus == "Approved" || x.ApprovalStatus == "Pending" || x.ApprovalStatus == "Approved")
                    && x.FromDate.Date <= dto.ToDate.Date
                    && x.ToDate.Date >= dto.FromDate.Date
                );

                if (hasOverlap)
                    throw new ArgumentException("Leave application overlaps with existing pending/approved leave.");

                // ✅ (6) Compute TotalLeaveDays
                decimal totalLeaveDays = await CalculateLeaveDaysAsync(dto.FromDate.Date, dto.ToDate.Date, dto.IsHalfDay);


                if (totalLeaveDays <= 0)
                    throw new ArgumentException("TotalLeaveDays is invalid after calculation.");

                // ✅ (7) Check balance (no negative allowed here)
                if (balanceRow.BalanceLeaveDays < totalLeaveDays)
                    throw new ArgumentException("Insufficient leave balance.");

                // ✅ (8) Backdate rule
                if (dto.FromDate.Date < today)
                {
                    if (!annualPolicy.AllowBackdatedLeave)
                        throw new ArgumentException("Backdated leave is not allowed for this leave type.");

                    if (annualPolicy.BackdateLimitDays.HasValue)
                    {
                        var daysBack = (today - dto.FromDate.Date).Days;
                        if (daysBack > annualPolicy.BackdateLimitDays.Value)
                            throw new ArgumentException($"Backdated leave allowed only up to {annualPolicy.BackdateLimitDays.Value} days.");
                    }
                }

                // ✅ (9) Document Rule
                if (annualPolicy.RequiresDocument
                    && annualPolicy.DocumentRequiredAfterDays.HasValue
                    && totalLeaveDays > annualPolicy.DocumentRequiredAfterDays.Value)
                {
                    // ✅ If UPDATE, check existing docs + newly uploaded docs
                    if (dto.IdLeaveApplication > 0)
                    {
                        bool hasExistingDoc = await _dbContext.LeaveApplicationDocuments
                            .AnyAsync(d => d.IdLeaveApplication == dto.IdLeaveApplication);

                        bool hasNewDoc = dto.Documents != null && dto.Documents.Any(f => f != null && f.Length > 0);

                        if (!hasExistingDoc && !hasNewDoc)
                            throw new ArgumentException("Document is required for this leave duration. Please upload document.");
                    }
                    else
                    {
                        // ✅ If NEW application → must upload documents in same request
                        bool hasNewDoc = dto.Documents != null && dto.Documents.Any(f => f != null && f.Length > 0);

                        if (!hasNewDoc)
                            throw new ArgumentException("Document is required for this leave duration. Please upload document.");
                    }
                }


                // ✅ (10) Create / Update application
                LeaveApplications entity;

                if (dto.IdLeaveApplication > 0)
                {
                    entity = await _dbContext.LeaveApplications
                        .FirstOrDefaultAsync(x => x.IdLeaveApplication == dto.IdLeaveApplication);

                    if (entity == null)
                        throw new ArgumentException("Leave application not found.");

                    // ✅ Only allow update if still pending
                    if (entity.ApprovalStatus != "Pending")
                        throw new ArgumentException("Only pending leave applications can be updated.");

                    entity.IdLeaveType = dto.IdLeaveType;
                    entity.LeaveTypeName = annualPolicy.LeaveTypeName;
                    entity.FromDate = dto.FromDate.Date;
                    entity.ToDate = dto.ToDate.Date;
                    entity.IsHalfDay = dto.IsHalfDay;
                    entity.HalfDayType = dto.HalfDayType;
                    entity.TotalLeaveDays = totalLeaveDays;
                    entity.Reason = dto.Reason.Trim();

                    // keep statuses same
                }
                else
                {
                    entity = new LeaveApplications
                    {
                        IdEmployee = dto.IdEmployee,
                        IdLeaveType = dto.IdLeaveType,
                        LeaveTypeName = annualPolicy.LeaveTypeName,
                        FromDate = dto.FromDate.Date,
                        ToDate = dto.ToDate.Date,
                        IsHalfDay = dto.IsHalfDay,
                        HalfDayType = dto.HalfDayType,
                        TotalLeaveDays = totalLeaveDays,
                        Reason = dto.Reason.Trim(),

                        AppliedOn = DateTime.Now,
                        ApprovalStatus = "Pending",
                        ApplicationStatus = "Submitted"
                    };

                    await _dbContext.LeaveApplications.AddAsync(entity);
                    await _dbContext.SaveChangesAsync();
                }

                await _dbContext.SaveChangesAsync();

                // ✅ Save documents if provided
                if (dto.Documents != null && dto.Documents.Any(f => f != null && f.Length > 0))
                {
                    if (dto.IdLeaveApplication > 0)
                    {
                        var existingDocs = await _dbContext.LeaveApplicationDocuments
                            .Where(x => x.IdLeaveApplication == dto.IdLeaveApplication)
                            .ToListAsync();

                        foreach (var doc in existingDocs)
                        {
                            if (!string.IsNullOrWhiteSpace(doc.FilePath) && File.Exists(doc.FilePath))
                                File.Delete(doc.FilePath);
                        }

                        _dbContext.LeaveApplicationDocuments.RemoveRange(existingDocs);
                        await _dbContext.SaveChangesAsync();
                    }
                    foreach (var file in dto.Documents)
                    {
                        if (file == null || file.Length == 0) continue;

                        var savedPath = await SaveLeaveApplicationFileAsync(file);

                        var docEntity = new LeaveApplicationDocuments
                        {
                            IdLeaveApplication = entity.IdLeaveApplication,
                            FileType = Path.GetExtension(file.FileName).Replace(".", "").ToUpperInvariant(),
                            FileName = file.FileName,  // ✅ original file name
                            FilePath = savedPath,
                            UploadedAt = DateTime.Now
                        };

                        await _dbContext.LeaveApplicationDocuments.AddAsync(docEntity);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        }

        public async Task<bool> DeleteLeaveApplicationDocument(
    int idLeaveApplicationDocument,
    int loggedInEmployeeId,
    bool isHrOverride)
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
                bool isSentBack = appStatus == "REJECTED" ;
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

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "leaveapplications");
            Directory.CreateDirectory(folderPath);

            // ✅ delete old file if exists (optional)
            if (!string.IsNullOrWhiteSpace(oldFilePath) && File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }

            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        private async Task<decimal> CalculateLeaveDaysAsync(DateTime fromDate, DateTime toDate, bool isHalfDay)
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
                if (holidaySet.Contains(date))
                    continue;

                count++;
            }

            return count;
        }

        public async Task<CancelLeaveApplicationResultDto> CancelLeaveApplication(
    int idLeaveApplication,
    string? cancelReason,
    int loggedInEmployeeId)
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

                // ✅ Block cancel if already approved
                if (approvalStatus == "APPROVED")
                    throw new ArgumentException("Approved leave cannot be cancelled. Please contact HR.");

                // ✅ Allow cancel only if Pending/Submitted/SentBack
                bool canCancel =
                    approvalStatus == "PENDING" ||
                    appStatus == "SUBMITTED" ||
                    appStatus == "SENTBACK" ||
                    approvalStatus == "SENTBACK";

                if (!canCancel)
                    throw new ArgumentException("This leave application cannot be cancelled.");

                // ✅ Cancel the leave
                application.ApplicationStatus = "Cancelled";
                application.CancelledDate = DateTime.Now;
                application.ReasonForCancellation = string.IsNullOrWhiteSpace(cancelReason)
                    ? "Cancelled by employee"
                    : cancelReason.Trim();

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



        #endregion
    }

}
