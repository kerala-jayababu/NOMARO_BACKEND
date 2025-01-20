using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChildTaxThresholdService : IChildTaxThresholdService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ChildTaxThresholdService> _logger;

    public ChildTaxThresholdService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ChildTaxThresholdService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ChildTaxThresholdDto>> GetAllChildTaxThresholds()
    {
        try
        {
            var thresholds = await _dbContext.ChildTaxThresholds.ToListAsync();
            return _mapper.Map<IEnumerable<ChildTaxThresholdDto>>(thresholds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all child tax thresholds.");
            throw;
        }
    }

    public async Task<ChildTaxThresholdDto?> GetChildTaxThresholdById(int id)
    {
        try
        {
            var threshold = await _dbContext.ChildTaxThresholds.FirstOrDefaultAsync(x => x.IdChildTaxThreshold == id);
            return threshold == null ? null : _mapper.Map<ChildTaxThresholdDto>(threshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching child tax threshold with ID: {Id}", id);
            throw;
        }
    }

    public async Task<ChildTaxThresholdDto?> AddChildTaxThreshold(ChildTaxThresholdDto dto)
    {
        try
        {
            var entity = _mapper.Map<ChildTaxThreshold>(dto);
            var addedEntity = await _dbContext.ChildTaxThresholds.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ChildTaxThresholdDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding child tax threshold.");
            return null;
        }
    }

    public async Task<ChildTaxThresholdDto?> UpdateChildTaxThreshold(ChildTaxThresholdDto dto)
    {
        try
        {
            var threshold = await _dbContext.ChildTaxThresholds.FirstOrDefaultAsync(x => x.IdChildTaxThreshold == dto.IdChildTaxThreshold);
            if (threshold == null) return null;

            threshold.IdTaxConfig = dto.IdTaxConfig;
            threshold.ChildrenCount = dto.ChildrenCount;
            threshold.TaxThresholdAmount = dto.TaxThresholdAmount;

            var updatedEntity = _dbContext.ChildTaxThresholds.Update(threshold);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ChildTaxThresholdDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating child tax threshold with ID: {Id}", dto.IdChildTaxThreshold);
            return null;
        }
    }
}
