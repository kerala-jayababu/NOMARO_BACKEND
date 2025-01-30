using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class SalaryTemplateDetailsService : ISalaryTemplateDetailsService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryTemplateDetailsService> _logger;

        public SalaryTemplateDetailsService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryTemplateDetailsService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SalaryTemplateDetailDto>> GetAllSalaryTemplateDetails()
        {
            try
            {
                var details = await _dbContext.SalaryTemplateDetails.ToListAsync();
                return _mapper.Map<IEnumerable<SalaryTemplateDetailDto>>(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary template details.");
                throw;
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

                existingDetail.IdSalaryTemplate = dto.IdSalaryTemplate;
                existingDetail.IdSalaryHead = dto.IdSalaryHead;
                existingDetail.CalculationMethod = dto.CalculationMethod;
                existingDetail.FixedAmount = dto.FixedAmount;
                existingDetail.PercentageOfIdSalaryHead = dto.PercentageOfIdSalaryHead;
                existingDetail.PercentageValue = dto.PercentageValue;
                existingDetail.CustomFormula = dto.CustomFormula;
                existingDetail.FinalSalaryAmount = dto.FinalSalaryAmount;
                existingDetail.Remarks = dto.Remarks;

                _dbContext.SalaryTemplateDetails.Update(existingDetail);
                await _dbContext.SaveChangesAsync();

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
