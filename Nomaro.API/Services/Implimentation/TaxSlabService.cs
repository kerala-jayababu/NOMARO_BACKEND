using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TaxSlabService : ITaxSlabService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxSlabService> _logger;
    private readonly IAuditService _auditService;

    public TaxSlabService(ApplicationDBContext dbContext, IMapper mapper, ILogger<TaxSlabService> logger, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>Slabs of a tax year configuration (financial year + regime), optionally for one age category.</summary>
    public async Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabs(int idTaxYearConfig, string? ageCategory = null)
    {
        try
        {
            var query = _dbContext.TaxSlabs.AsNoTracking().Where(ts => ts.IdTaxYearConfig == idTaxYearConfig);
            if (!string.IsNullOrWhiteSpace(ageCategory))
            {
                query = query.Where(ts => ts.AgeCategory == ageCategory);
            }

            var taxSlabs = await query
                .OrderBy(ts => ts.AgeCategory)
                .ThenBy(ts => ts.IncomeFrom)
                .ToListAsync();

            return _mapper.Map<List<TaxSlabDto>>(taxSlabs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax slabs.");
            throw;
        }
    }

    public async Task<TaxSlabDto?> GetTaxSlabById(int id)
    {
        try
        {
            var taxSlab = await _dbContext.TaxSlabs.AsNoTracking().FirstOrDefaultAsync(t => t.IdTaxSlab == id);
            return taxSlab == null ? null : _mapper.Map<TaxSlabDto>(taxSlab);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax slab by ID: {Id}", id);
            throw;
        }
    }

    public async Task<TaxSlabDto?> AddTaxSlab(TaxSlabDto taxSlabDto)
    {
        await ValidateSlab(taxSlabDto);

        try
        {
            var taxSlabEntity = new TaxSlab
            {
                IdTaxYearConfig = taxSlabDto.IdTaxYearConfig,
                AgeCategory = taxSlabDto.AgeCategory.Trim().ToUpper(),
                IncomeFrom = taxSlabDto.IncomeFrom,
                IncomeTo = taxSlabDto.IncomeTo,
                TaxRate = taxSlabDto.TaxRate
            };
            var addedEntity = await _dbContext.TaxSlabs.AddAsync(taxSlabEntity);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Create",
                entityName: "TaxSlab",
                entityId: addedEntity.Entity.IdTaxSlab,
                actionDetails: new { after = _mapper.Map<TaxSlabDto>(addedEntity.Entity) });

            return _mapper.Map<TaxSlabDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tax slab: {@TaxSlabDto}", taxSlabDto);
            return null;
        }
    }

    public async Task<TaxSlabDto?> UpdateTaxSlab(TaxSlabDto taxSlabDto)
    {
        var taxSlab = await _dbContext.TaxSlabs.FirstOrDefaultAsync(t => t.IdTaxSlab == taxSlabDto.IdTaxSlab);
        if (taxSlab == null) return null;

        await ValidateSlab(taxSlabDto);

        try
        {
            var beforeUpdate = _mapper.Map<TaxSlabDto>(taxSlab);

            taxSlab.IdTaxYearConfig = taxSlabDto.IdTaxYearConfig;
            taxSlab.AgeCategory = taxSlabDto.AgeCategory.Trim().ToUpper();
            taxSlab.IncomeFrom = taxSlabDto.IncomeFrom;
            taxSlab.IncomeTo = taxSlabDto.IncomeTo;
            taxSlab.TaxRate = taxSlabDto.TaxRate;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Update",
                entityName: "TaxSlab",
                entityId: taxSlab.IdTaxSlab,
                actionDetails: new { before = beforeUpdate, after = _mapper.Map<TaxSlabDto>(taxSlab) });

            return _mapper.Map<TaxSlabDto>(taxSlab);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax slab with ID: {Id}", taxSlabDto.IdTaxSlab);
            return null;
        }
    }

    /// <summary>
    /// The tax year configuration must exist, and within one configuration and age category the income ranges
    /// must not overlap (so only one open-ended "and above" slab). Throws InvalidOperationException with the user message.
    /// </summary>
    private async Task ValidateSlab(TaxSlabDto dto)
    {
        if (!await _dbContext.TaxYearConfigs.AsNoTracking().AnyAsync(c => c.IdTaxYearConfig == dto.IdTaxYearConfig))
            throw new InvalidOperationException("The selected tax year configuration does not exist.");

        var ageCategory = (dto.AgeCategory ?? string.Empty).Trim().ToUpper();
        var others = await _dbContext.TaxSlabs.AsNoTracking()
            .Where(ts => ts.IdTaxYearConfig == dto.IdTaxYearConfig && ts.AgeCategory == ageCategory && ts.IdTaxSlab != dto.IdTaxSlab)
            .ToListAsync();

        var newTo = dto.IncomeTo ?? decimal.MaxValue;
        foreach (var other in others)
        {
            var otherTo = other.IncomeTo ?? decimal.MaxValue;
            if (dto.IncomeFrom < otherTo && other.IncomeFrom < newTo)
            {
                var range = other.IncomeTo.HasValue
                    ? $"{other.IncomeFrom:N0} - {other.IncomeTo.Value:N0}"
                    : $"{other.IncomeFrom:N0} and above";
                throw new InvalidOperationException($"This income range overlaps the existing {ageCategory} slab {range}.");
            }
        }
    }
}
