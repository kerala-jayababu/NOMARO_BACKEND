using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class SalaryTemplateDetailsService : ISalaryTemplateDetailsService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryTemplateDetailsService> _logger;
        private readonly IAuditService _auditService;

        public SalaryTemplateDetailsService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryTemplateDetailsService> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        //public async Task<IEnumerable<SalaryTemplateDetailDto>> GetAllSalaryTemplateDetails(int Id)
        //{
        //    try
        //    {
        //        var details = await _dbContext.SalaryTemplateDetails.ToListAsync();
        //        return _mapper.Map<IEnumerable<SalaryTemplateDetailDto>>(details);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching salary template details.");
        //        throw;
        //    }
        //}

        public async Task<IEnumerable<SalaryTemplateDetailDto>> GetAllSalaryTemplateDetails(int idSalaryTemplate)
        {
            try
            {
                var result = await (from std in _dbContext.SalaryTemplateDetails
                                    join sh in _dbContext.SalaryHeads
                                    on std.IdSalaryHead equals sh.IdSalaryHead into shGroup
                                    from sh in shGroup.DefaultIfEmpty() // This makes it a LEFT JOIN
                                    where std.IdSalaryTemplate == idSalaryTemplate
                                    select new SalaryTemplateDetailDto
                                    {
                                        IdSalaryTemplateDetail = std.IdSalaryTemplateDetail,
                                        IdSalaryTemplate = (int)std.IdSalaryTemplate,
                                        IdSalaryHead = (int)std.IdSalaryHead,
                                        SalaryHeadName = sh.SalaryHeadName ?? string.Empty, // Use null-coalescing
                                        HeadType = sh.HeadType ?? string.Empty, // Use null-coalescing
                                        IsTaxable = sh != null && sh.IsTaxable, // Handle null for boolean
                                        OrderNumber = sh != null ? sh.OrderNumber : null, // Check null for nullable int
                                        CalculationMethod = sh.CalculationMethod,
                                        FixedAmount = std.FixedAmount,
                                        PercentageOfIdSalaryHead = sh.IdPercentageSalaryHead,
                                        PercentageValue = std.PercentageValue,
                                        CustomFormula = sh.CustomFormula,
                                        CalcSequence = sh.CalcSequence,
                                        FinalSalaryAmount = (decimal)std.FinalSalaryAmount,
                                        Remarks = std.Remarks
                                    }).ToListAsync();



                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Salary Template Details.");
                throw new Exception("An error occurred while fetching salary template details. Please try again later.");
            }
        }


        public async Task<SalaryTemplateDetailDto?> GetSalaryTemplateDetailById(int id)
        {
            try
            {
                var detail = await _dbContext.SalaryTemplateDetails.FirstOrDefaultAsync(d => d.IdSalaryTemplateDetail == id);
                return detail == null ? null : _mapper.Map<SalaryTemplateDetailDto>(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary template detail with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SalaryTemplateDetailDto?> AddSalaryTemplateDetail(SalaryTemplateDetailDto dto)
        {
            try
            {
                var entity = _mapper.Map<SalaryTemplateDetails>(dto);
                var addedEntity = await _dbContext.SalaryTemplateDetails.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync("Create", "SalaryTemplateDetail", (int)addedEntity.Entity.IdSalaryTemplateDetail, dto);
                return _mapper.Map<SalaryTemplateDetailDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding salary template detail: {@Dto}", dto);
                return null;
            }
        }

        public async Task<SalaryTemplateDetailDto?> UpdateSalaryTemplateDetail(SalaryTemplateDetailDto dto)
        {
            try
            {
                var existingDetail = await _dbContext.SalaryTemplateDetails.FirstOrDefaultAsync(d => d.IdSalaryTemplateDetail == dto.IdSalaryTemplateDetail);
                if (existingDetail == null) return null;

                var beforeUpdate = _mapper.Map<SalaryTemplateDetailDto>(existingDetail);

                existingDetail.IdSalaryTemplate = dto.IdSalaryTemplate;
                existingDetail.IdSalaryHead = dto.IdSalaryHead;
                existingDetail.FixedAmount = dto.FixedAmount;
                existingDetail.PercentageValue = dto.PercentageValue;
                existingDetail.FinalSalaryAmount = dto.FinalSalaryAmount;
                existingDetail.Remarks = dto.Remarks;

                _dbContext.SalaryTemplateDetails.Update(existingDetail);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync("Update", "SalaryTemplateDetail", (int)existingDetail.IdSalaryTemplateDetail, new { before = beforeUpdate, after = dto });
                return _mapper.Map<SalaryTemplateDetailDto>(existingDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salary template detail with ID: {Id}", dto.IdSalaryTemplateDetail);
                return null;
            }
        }
    }

}

