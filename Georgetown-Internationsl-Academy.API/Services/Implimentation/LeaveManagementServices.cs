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
                var templatesQuery = _dbContext.LeaveTemplates.AsQueryable();

                if (idLeaveTemplate.HasValue)
                    templatesQuery = templatesQuery.Where(x => x.IdLeaveTemplate == idLeaveTemplate.Value);

                if (isActive.HasValue)
                    templatesQuery = templatesQuery.Where(x => x.IsActive == isActive.Value);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var text = searchText.Trim();
                    templatesQuery = templatesQuery.Where(x =>
                        x.LeaveTemplateName.Contains(text) ||
                        (x.LeaveTemplateDesc != null && x.LeaveTemplateDesc.Contains(text)));
                }

                var result = await (
                    from t in templatesQuery
                    join c in _dbContext.AnnualLeaveTypeConfig
                        on t.IdAnnualLeaveTypeConfig equals c.IdAnnualLeaveTypeConfig into cfg
                    from config in cfg.DefaultIfEmpty()
                    orderby t.LeaveTemplateName
                    select new LeaveTemplateDto
                    {
                        IdLeaveTemplate = t.IdLeaveTemplate,
                        LeaveTemplateName = t.LeaveTemplateName,
                        IdAnnualLeaveTypeConfig = t.IdAnnualLeaveTypeConfig,

                        AnnualLeaveTypeConfig = config == null ? null : new AnnualLeaveTypeConfigDto
                        {
                            IdAnnualLeaveTypeConfig = config.IdAnnualLeaveTypeConfig,
                            IdLeaveType = config.IdLeaveType,
                            LeaveTypeName = config.LeaveTypeName,
                            LeaveCode = config.LeaveCode,
                            IdYear = config.IdYear,
                            EffectiveFrom = config.EffectiveFrom,
                            EffectiveTo = config.EffectiveTo,
                            IsPaid = config.IsPaid,
                            SalaryDeductionPercent = config.SalaryDeductionPercent,
                            AllowHalfDay = config.AllowHalfDay,
                            RequiresApproval = config.RequiresApproval,
                            RequiredApprovalLevel = config.RequiredApprovalLevel,
                            RequiresDocument = config.RequiresDocument,
                            DocumentRequiredAfterDays = config.DocumentRequiredAfterDays,
                            IsCarryForwardAllowed = config.IsCarryForwardAllowed,
                            MaxCarryForwardDays = config.MaxCarryForwardDays,
                            ApplicableGender = config.ApplicableGender,
                            IsActive = config.IsActive,
                            IncludeHolidaysBetween = config.IncludeHolidaysBetween,
                            MaxLeavesPerYear = config.MaxLeavesPerYear,
                            MaxLeavesPerMonth = config.MaxLeavesPerMonth,
                            AllowBackdatedLeave = config.AllowBackdatedLeave,
                            BackdateLimitDays = config.BackdateLimitDays,
                            CreatedBy = config.CreatedBy,
                            CreatedAt = config.CreatedAt,
                            UpdatedBy = config.UpdatedBy,
                            UpdatedAt = config.UpdatedAt
                        },

                        IsActive = t.IsActive,
                        CreatedBy = t.CreatedBy,
                        CreatedAt = t.CreatedAt,
                        UpdatedBy = t.UpdatedBy,
                        UpdatedAt = t.UpdatedAt
                    }
                ).ToListAsync();

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
    }

}
