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
            var leaveTypes = await _dbContext.LeaveTypes
                .OrderBy(x => x.LeaveTypeName).ToListAsync();

            return (IEnumerable<LeaveTypesDto>)leaveTypes;
        }

        public async Task<bool> AddOrUpdateLeaveTypes(List<LeaveTypesDto> leaveTypeDtoList)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingLeaveTypes = await _dbContext.LeaveTypes.ToListAsync();
                var updatedLeaveTypes = new List<LeaveTypes>();

                foreach (var leaveTypeDto in leaveTypeDtoList)
                {
                    var existingLeaveType = existingLeaveTypes
                        .FirstOrDefault(x => x.IdLeaveType == leaveTypeDto.IdLeaveType);

                    if (existingLeaveType != null)
                    {
                        // ✅ Update existing
                        existingLeaveType.LeaveCode = leaveTypeDto.LeaveCode;
                        existingLeaveType.LeaveTypeName = leaveTypeDto.LeaveTypeName;

                        updatedLeaveTypes.Add(existingLeaveType);
                    }
                    else
                    {
                        // ✅ Add new
                        var newLeaveType = new LeaveTypes
                        {
                            LeaveCode = leaveTypeDto.LeaveCode,
                            LeaveTypeName = leaveTypeDto.LeaveTypeName
                        };

                        await _dbContext.LeaveTypes.AddAsync(newLeaveType);
                        updatedLeaveTypes.Add(newLeaveType);
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
                throw new Exception("An error occurred while processing leave types. Please try again.");
            }
        }


        #endregion

        #region AnnualLeaveTypeConfig

        public async Task<IEnumerable<AnnualLeaveTypeConfigDto>> GetAnnualLeaveTypeConfigs(int idYear)
        {
            var configs = await _dbContext.AnnualLeaveTypeConfig
                .Where(x => x.IdYear == idYear)
                .OrderBy(x => x.LeaveTypeName)
                .ToListAsync();

            return (IEnumerable<AnnualLeaveTypeConfigDto>)configs;
        }

        public async Task<bool> AddOrUpdateAnnualLeaveTypeConfigs(
        List<AnnualLeaveTypeConfigDto> configDtoList)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingConfigs = await _dbContext.AnnualLeaveTypeConfig.ToListAsync();
                var updatedConfigs = new List<AnnualLeaveTypeConfig>();

                foreach (var configDto in configDtoList)
                {
                    var existingConfig = existingConfigs
                        .FirstOrDefault(x =>
                            x.IdAnnualLeaveTypeConfig == configDto.IdAnnualLeaveTypeConfig);

                    if (existingConfig != null)
                    {
                        // ✅ Update existing
                        existingConfig.IdLeaveType = configDto.IdLeaveType;
                        existingConfig.LeaveTypeName = configDto.LeaveTypeName;
                        existingConfig.LeaveCode = configDto.LeaveCode;
                        existingConfig.IdYear = configDto.IdYear;

                        existingConfig.EffectiveFrom = configDto.EffectiveFrom;
                        existingConfig.EffectiveTo = configDto.EffectiveTo;

                        existingConfig.IsPaid = configDto.IsPaid;
                        existingConfig.SalaryDeductionPercent = configDto.SalaryDeductionPercent;

                        existingConfig.AllowHalfDay = configDto.AllowHalfDay;
                        existingConfig.RequiresApproval = configDto.RequiresApproval;
                        existingConfig.RequiredApprovalLevel = configDto.RequiredApprovalLevel;

                        existingConfig.RequiresDocument = configDto.RequiresDocument;
                        existingConfig.DocumentRequiredAfterDays = configDto.DocumentRequiredAfterDays;

                        existingConfig.IsCarryForwardAllowed = configDto.IsCarryForwardAllowed;
                        existingConfig.MaxCarryForwardDays = configDto.MaxCarryForwardDays;

                        existingConfig.ApplicableGender = configDto.ApplicableGender;
                        existingConfig.IsActive = configDto.IsActive;

                        existingConfig.IncludeHolidaysBetween = configDto.IncludeHolidaysBetween;

                        existingConfig.MaxLeavesPerYear = configDto.MaxLeavesPerYear;
                        existingConfig.MaxLeavesPerMonth = configDto.MaxLeavesPerMonth;

                        existingConfig.AllowBackdatedLeave = configDto.AllowBackdatedLeave;
                        existingConfig.BackdateLimitDays = configDto.BackdateLimitDays;

                        existingConfig.UpdatedBy = configDto.UpdatedBy;
                        existingConfig.UpdatedAt = DateTime.Now;

                        updatedConfigs.Add(existingConfig);
                    }
                    else
                    {
                        // ✅ Add new
                        var newConfig = new AnnualLeaveTypeConfig
                        {
                            IdLeaveType = configDto.IdLeaveType,
                            LeaveTypeName = configDto.LeaveTypeName,
                            LeaveCode = configDto.LeaveCode,
                            IdYear = configDto.IdYear,

                            EffectiveFrom = configDto.EffectiveFrom,
                            EffectiveTo = configDto.EffectiveTo,

                            IsPaid = configDto.IsPaid,
                            SalaryDeductionPercent = configDto.SalaryDeductionPercent,

                            AllowHalfDay = configDto.AllowHalfDay,
                            RequiresApproval = configDto.RequiresApproval,
                            RequiredApprovalLevel = configDto.RequiredApprovalLevel,

                            RequiresDocument = configDto.RequiresDocument,
                            DocumentRequiredAfterDays = configDto.DocumentRequiredAfterDays,

                            IsCarryForwardAllowed = configDto.IsCarryForwardAllowed,
                            MaxCarryForwardDays = configDto.MaxCarryForwardDays,

                            ApplicableGender = configDto.ApplicableGender,
                            IsActive = configDto.IsActive,

                            IncludeHolidaysBetween = configDto.IncludeHolidaysBetween,

                            MaxLeavesPerYear = configDto.MaxLeavesPerYear,
                            MaxLeavesPerMonth = configDto.MaxLeavesPerMonth,

                            AllowBackdatedLeave = configDto.AllowBackdatedLeave,
                            BackdateLimitDays = configDto.BackdateLimitDays,

                            CreatedBy = configDto.CreatedBy,
                            CreatedAt = DateTime.Now
                        };

                        await _dbContext.AnnualLeaveTypeConfig.AddAsync(newConfig);
                        updatedConfigs.Add(newConfig);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating AnnualLeaveTypeConfig.");
                throw new Exception(
                    "An error occurred while processing annual leave configurations. Please try again.");
            }
        }


        #endregion
    }
}
