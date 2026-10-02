using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class TaxYearConfigService : ITaxYearConfigService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxYearConfigService> _logger;
    private readonly IAuditService _auditService;

    public TaxYearConfigService(ApplicationDBContext dbContext, IMapper mapper, ILogger<TaxYearConfigService> logger, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>Tax year configurations (one per financial year and regime) with their financial year and slab count.</summary>
    public async Task<IEnumerable<TaxYearConfigDto>> GetTaxYearConfigs(int? idFinancialYear = null)
    {
        try
        {
            var query = _dbContext.TaxYearConfigs.AsNoTracking().AsQueryable();
            if (idFinancialYear.HasValue)
            {
                query = query.Where(c => c.IdFinancialYear == idFinancialYear.Value);
            }

            var rows = await (
                from c in query
                join fy in _dbContext.FinancialYears.AsNoTracking() on c.IdFinancialYear equals (int?)fy.IdFinancialYear into fyGroup
                from fy in fyGroup.DefaultIfEmpty()
                select new TaxYearConfigDto
                {
                    IdTaxYearConfig = c.IdTaxYearConfig,
                    IdFinancialYear = c.IdFinancialYear ?? 0,
                    IdTaxRegime = c.IdTaxRegime,
                    StandardDeduction = c.StandardDeduction,
                    RebateIncomeLimit = c.RebateIncomeLimit,
                    RebateMaxAmount = c.RebateMaxAmount,
                    CessRate = c.CessRate,
                    AllowMarginalRelief = c.AllowMarginalRelief,
                    FinancialYear = c.FinancialYear,
                    FinancialYearName = fy != null ? fy.FinancialYearName : null,
                    FinancialYearFrom = fy != null ? fy.FinancialYearFrom : null,
                    FinancialYearTo = fy != null ? fy.FinancialYearTo : null,
                    SlabCount = _dbContext.TaxSlabs.Count(s => s.IdTaxYearConfig == c.IdTaxYearConfig)
                })
                .OrderByDescending(x => x.FinancialYearFrom)
                .ThenBy(x => x.IdTaxRegime)
                .ToListAsync();

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax year configurations.");
            throw;
        }
    }

    public async Task<TaxYearConfigDto?> GetTaxYearConfigById(int id)
    {
        return (await GetTaxYearConfigs()).FirstOrDefault(c => c.IdTaxYearConfig == id);
    }

    /// <summary>
    /// Adds or updates a configuration. One configuration per financial year and regime.
    /// Throws InvalidOperationException with the user message.
    /// </summary>
    public async Task<bool> AddOrUpdateTaxYearConfig(TaxYearConfigDto dto)
    {
        var financialYear = await _dbContext.FinancialYears.AsNoTracking()
            .FirstOrDefaultAsync(fy => fy.IdFinancialYear == dto.IdFinancialYear);
        if (financialYear == null)
            throw new InvalidOperationException("The selected financial year does not exist.");

        var duplicate = await _dbContext.TaxYearConfigs.AsNoTracking()
            .AnyAsync(c => c.IdFinancialYear == dto.IdFinancialYear && c.IdTaxRegime == dto.IdTaxRegime && c.IdTaxYearConfig != dto.IdTaxYearConfig);
        if (duplicate)
            throw new InvalidOperationException("This financial year already has a configuration for the selected tax regime.");

        // Text form kept in the FinancialYear column, e.g. "2026-27"
        var financialYearText = financialYear.FinancialYearFrom.HasValue && financialYear.FinancialYearTo.HasValue
            ? $"{financialYear.FinancialYearFrom.Value.Year}-{(financialYear.FinancialYearTo.Value.Year % 100):00}"
            : financialYear.FinancialYearName ?? string.Empty;

        try
        {
            var entity = await _dbContext.TaxYearConfigs.FirstOrDefaultAsync(c => c.IdTaxYearConfig == dto.IdTaxYearConfig);
            var isUpdate = entity != null;
            TaxYearConfigDto? before = isUpdate ? _mapper.Map<TaxYearConfigDto>(entity) : null;

            if (entity == null)
            {
                entity = new TaxYearConfigs();
                await _dbContext.TaxYearConfigs.AddAsync(entity);
            }

            entity.IdFinancialYear = dto.IdFinancialYear;
            entity.FinancialYear = financialYearText;
            entity.IdTaxRegime = dto.IdTaxRegime;
            entity.StandardDeduction = dto.StandardDeduction;
            entity.RebateIncomeLimit = dto.RebateIncomeLimit;
            entity.RebateMaxAmount = dto.RebateMaxAmount;
            entity.CessRate = dto.CessRate;
            entity.AllowMarginalRelief = dto.AllowMarginalRelief;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: isUpdate ? "Update" : "Create",
                entityName: "TaxYearConfig",
                entityId: entity.IdTaxYearConfig,
                actionDetails: new { before, after = _mapper.Map<TaxYearConfigDto>(entity) });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tax year configuration: {@Dto}", dto);
            return false;
        }
    }
}
