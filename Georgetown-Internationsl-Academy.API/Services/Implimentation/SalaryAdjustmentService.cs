
using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{

    public class SalaryAdjustmentService : ISalaryAdjustmentService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryAdjustmentService> _logger;

        public SalaryAdjustmentService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryAdjustmentService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SalaryAdjustmentDto>> GetAllSalaryAdjustments()
        {
            try
            {
                var adjustments = await _dbContext.SalaryAdjustments.ToListAsync();
                return _mapper.Map<IEnumerable<SalaryAdjustmentDto>>(adjustments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all salary adjustments.");
                throw;
            }
        }

        public async Task<SalaryAdjustmentDto?> GetSalaryAdjustmentById(int id)
        {
            try
            {
                var adjustment = await _dbContext.SalaryAdjustments.FirstOrDefaultAsync(x => x.IdSalaryAdjustment == id);
                return adjustment == null ? null : _mapper.Map<SalaryAdjustmentDto>(adjustment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary adjustment with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SalaryAdjustmentDto?> AddSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment)
        {
            try
            {
                var entity = _mapper.Map<SalaryAdjustment>(salaryAdjustment);
                var addedEntity = await _dbContext.SalaryAdjustments.AddAsync(entity);
                await _dbContext.SaveChangesAsync();
                return _mapper.Map<SalaryAdjustmentDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding salary adjustment.");
                return null;
            }
        }

        public async Task<SalaryAdjustmentDto?> UpdateSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment)
        {
            try
            {
                var existingAdjustment = await _dbContext.SalaryAdjustments.FirstOrDefaultAsync(x => x.IdSalaryAdjustment == salaryAdjustment.IdSalaryAdjustment);
                if (existingAdjustment == null) return null;

                _mapper.Map(salaryAdjustment, existingAdjustment);
                _dbContext.SalaryAdjustments.Update(existingAdjustment);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<SalaryAdjustmentDto>(existingAdjustment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salary adjustment.");
                return null;
            }
        }
    }

}
