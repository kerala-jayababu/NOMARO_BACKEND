using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class ScheduledSalaryDeductionService : IScheduledSalaryDeductionService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ScheduledSalaryDeductionService> _logger;

    public ScheduledSalaryDeductionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ScheduledSalaryDeductionService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ScheduledSalaryDeductionDto>> GetScheduledDeductions()
    {
        try
        {
            var deductions = await _dbContext.ScheduledDeductions.ToListAsync();
            return _mapper.Map<IEnumerable<ScheduledSalaryDeductionDto>>(deductions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scheduled salary deductions.");
            throw;
        }
    }

    public async Task<ScheduledSalaryDeductionDto?> GetScheduledDeductionById(int id)
    {
        try
        {
            var deduction = await _dbContext.ScheduledDeductions.FindAsync(id);
            return deduction == null ? null : _mapper.Map<ScheduledSalaryDeductionDto>(deduction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scheduled salary deduction with ID: {Id}", id);
            throw;
        }
    }

    public async Task<ScheduledSalaryDeductionDto?> AddScheduledDeduction(ScheduledSalaryDeductionDto dto)
    {
        try
        {
            var entity = _mapper.Map<ScheduledSalaryDeduction>(dto);
            var addedEntity = await _dbContext.ScheduledDeductions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ScheduledSalaryDeductionDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding scheduled salary deduction.");
            return null;
        }
    }

    public async Task<ScheduledSalaryDeductionDto?> UpdateScheduledDeduction(ScheduledSalaryDeductionDto dto)
    {
        try
        {
            var entity = await _dbContext.ScheduledDeductions.FindAsync(dto.IdScheduledSalaryDeduction);
            if (entity == null) return null;

            _mapper.Map(dto, entity);
            _dbContext.ScheduledDeductions.Update(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ScheduledSalaryDeductionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled salary deduction.");
            return null;
        }
    }
}
