using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TaxConfigService : ITaxConfigService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxConfigService> _logger;

    public TaxConfigService(ApplicationDBContext dbContext, IMapper mapper, ILogger<TaxConfigService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TaxConfigDto>> GetAllTaxConfigs()
    {
        try
        {
            var taxConfigs = await _dbContext.TaxConfigs.ToListAsync();
            return _mapper.Map<IEnumerable<TaxConfigDto>>(taxConfigs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax configs.");
            throw;
        }
    }

    public async Task<TaxConfigDto?> GetTaxConfigById(int id)
    {
        try
        {
            var taxConfig = await _dbContext.TaxConfigs.FirstOrDefaultAsync(tc => tc.IdTaxConfig == id);
            return taxConfig == null ? null : _mapper.Map<TaxConfigDto>(taxConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax config with ID: {Id}", id);
            throw;
        }
    }

    public async Task<TaxConfigManageDto?> AddTaxConfig(TaxConfigManageDto taxConfigDto,int IdEmployee)
    {
        try
        {
            var taxConfigEntity = _mapper.Map<TaxConfig>(taxConfigDto);
            taxConfigEntity.CreatedOn = DateTime.UtcNow;
            taxConfigEntity.CreatedBy = IdEmployee;
            var addedEntity = await _dbContext.TaxConfigs.AddAsync(taxConfigEntity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TaxConfigManageDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tax config: {@TaxConfigDto}", taxConfigDto);
            return null;
        }
    }

    public async Task<TaxConfigManageDto?> UpdateTaxConfig(TaxConfigManageDto taxConfigDto,int IdEmployee)
    {
        try
        {
            var taxConfig = await _dbContext.TaxConfigs.FirstOrDefaultAsync(tc => tc.IdTaxConfig == taxConfigDto.IdTaxConfig);

            if (taxConfig == null)
            {
                _logger.LogWarning("Tax config with ID {Id} not found.", taxConfigDto.IdTaxConfig);
                return null;
            }

            taxConfig.FinancialYearDesc = taxConfigDto.FinancialYearDesc;
            taxConfig.ValidFrom = taxConfigDto.ValidFrom;
            taxConfig.ValidTo = taxConfigDto.ValidTo;
            taxConfig.ModifiedOn = DateTime.UtcNow;
            taxConfig.ModifiedBy = IdEmployee;

            var updatedEntity = _dbContext.TaxConfigs.Update(taxConfig);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TaxConfigManageDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax config with ID: {Id}", taxConfigDto.IdTaxConfig);
            return null;
        }
    }
}
