using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using static Georgetown_Internationsl_Academy.API.Services.Implimentation.EmployeeOffBoardingService;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class EmployeeOffBoardingService : IEmployeeOffBoarding
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<AssetServices> _logger;

        public EmployeeOffBoardingService(ApplicationDBContext dbContext,IMapper mapper,ILogger<AssetServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region OFFBOARDING CONFIGURATIONS

        public async Task<IEnumerable<ExitReasonDto>> GetExitReasons()
        {
            var data = await _dbContext.ExitReasons
                .OrderBy(x => x.ReasonName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ExitReasonDto>>(data);
        }

        public async Task<bool> AddOrUpdateExitReasons(List<ExitReasonDto> dtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ExitReasons.ToListAsync();

                foreach (var dto in dtos)
                {
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

        public async Task<bool> AddOrUpdateExitTypes(List<ExitTypeDto> dtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ExitTypes.ToListAsync();

                foreach (var dto in dtos)
                {
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

        public async Task<bool> AddOrUpdateNoticePeriodPolicies(List<NoticePeriodPolicyDto> dtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.NoticePeriodPolicies.ToListAsync();

                foreach (var dto in dtos)
                {
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

        public async Task<bool> AddOrUpdateClearanceTemplates(List<ClearanceTemplateDto> dtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.ClearanceTemplates.ToListAsync();

                foreach (var dto in dtos)
                {
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
        #endregion

    }

}
