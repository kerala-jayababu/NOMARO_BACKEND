using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TaxSlabService : ITaxSlabService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxSlabService> _logger;

    public TaxSlabService(ApplicationDBContext dbContext, IMapper mapper, ILogger<TaxSlabService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
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
               
                var currentDate = DateTime.UtcNow.Date;
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
            taxSlab.TaxRate = taxSlabDto.TaxRate;

            var updatedEntity = _dbContext.TaxSlabs.Update(taxSlab);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TaxSlabDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax slab with ID: {Id}", taxSlabDto.IdTaxSlab);
            return null;
        }
    }
}
