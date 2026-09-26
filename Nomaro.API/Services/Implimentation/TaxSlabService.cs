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

    public async Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabs(int? idFinancialYear = null)
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
               
                var currentDate = DateTime.Now.Date;
                var financialYear = await _dbContext.FinancialYears
                    .Where(fy => fy.FinancialYearFrom <= currentDate && fy.FinancialYearTo >= currentDate)
                    .FirstOrDefaultAsync();

                if (financialYear == null)
                    return Enumerable.Empty<TaxSlabDto>(); 

                currentFinancialYearId = financialYear.IdFinancialYear;
            }

 
            // Fetch tax slabs along with financial year details
            var taxSlabs = await _dbContext.TaxSlabs
                .Where(ts => ts.IdFinancialYear == currentFinancialYearId)
                .Join(_dbContext.FinancialYears,
                      ts => ts.IdFinancialYear,
                      fy => fy.IdFinancialYear,
                      (ts, fy) => new TaxSlabDto
                      {
                          IdTaxSlab = ts.IdTaxSlab,
                          MinAmount = ts.MinAmount,
                          MaxAmount = ts.MaxAmount,
                          TaxRate = ts.TaxRate,
                          IdFinancialYear = ts.IdFinancialYear,
                          FinancialYearFrom = fy.FinancialYearFrom,
                          FinancialYearTo = fy.FinancialYearTo
                      })
                .ToListAsync();

            return taxSlabs;
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
            var taxSlab = await _dbContext.TaxSlabs.FirstOrDefaultAsync(t => t.IdTaxSlab == id);
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
        try
        {
            var taxSlabEntity = _mapper.Map<TaxSlab>(taxSlabDto);
            var addedEntity = await _dbContext.TaxSlabs.AddAsync(taxSlabEntity);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Create",
                entityName: "TaxSlab",
                entityId: addedEntity.Entity.IdTaxSlab,
                actionDetails: new { after = addedEntity.Entity });

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
        try
        {
            var taxSlab = await _dbContext.TaxSlabs.FirstOrDefaultAsync(t => t.IdTaxSlab == taxSlabDto.IdTaxSlab);
            if (taxSlab == null) return null;

            taxSlab.MinAmount = taxSlabDto.MinAmount;
            taxSlab.MaxAmount = taxSlabDto.MaxAmount;
            var beforeUpdate = _mapper.Map<TaxSlabDto>(taxSlab);

            taxSlab.TaxRate = taxSlabDto.TaxRate;

            var updatedEntity = _dbContext.TaxSlabs.Update(taxSlab);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Update",
                entityName: "TaxSlab",
                entityId: updatedEntity.Entity.IdTaxSlab,
                actionDetails: new { before = beforeUpdate, after = _mapper.Map<TaxSlabDto>(updatedEntity.Entity) });

            return _mapper.Map<TaxSlabDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax slab with ID: {Id}", taxSlabDto.IdTaxSlab);
            return null;
        }
    }
}

