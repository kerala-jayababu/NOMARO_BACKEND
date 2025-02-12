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
    public async Task<IEnumerable<ChildTaxThresholdDto>> GetAllChildTaxThresholds(int? idFinancialYear = null)
    {
        try
        {
            int currentFinancialYearId;

            if (idFinancialYear.HasValue)
            {
                // Use the provided IdFinancialYear
                currentFinancialYearId = idFinancialYear.Value;
            }
            else
            {
                // Determine current financial year based on today's date
                var currentDate = DateTime.UtcNow.Date;
                var financialYear = await _dbContext.FinancialYears
                    .Where(fy => fy.FinancialYearFrom <= currentDate && fy.FinancialYearTo >= currentDate)
                    .FirstOrDefaultAsync();

                if (financialYear == null)
                    return Enumerable.Empty<ChildTaxThresholdDto>(); 

                currentFinancialYearId = financialYear.IdFinancialYear;
            }

         
            var thresholds = await _dbContext.ChildTaxThresholds
                .Where(ctt => ctt.IdFinancialYear == currentFinancialYearId)
                .Join(_dbContext.FinancialYears,
                      ctt => ctt.IdFinancialYear,
                      fy => fy.IdFinancialYear,
                      (ctt, fy) => new ChildTaxThresholdDto
                      {
                          IdChildTaxThreshold = ctt.IdChildTaxThreshold,
                          ChildrenCount = ctt.ChildrenCount,
                          TaxThresholdAmount = ctt.TaxThresholdAmount,
                          IdFinancialYear = ctt.IdFinancialYear,
                          FinancialYearFrom = fy.FinancialYearFrom,
                          FinancialYearTo = fy.FinancialYearTo
                      })
                .ToListAsync();

            return thresholds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching child tax thresholds.");
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

            threshold.IdFinancialYear = dto.IdFinancialYear;
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
