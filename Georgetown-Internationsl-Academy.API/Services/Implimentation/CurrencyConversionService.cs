using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<CurrencyConversionService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<CurrencyConversionDto>> GetAllCurrencyConversions()
    {
        try
        {
            var conversions = await _dbContext.CurrencyConversions.ToListAsync();
            return _mapper.Map<IEnumerable<CurrencyConversionDto>>(conversions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching the list of currency conversions.");
            throw;
        }
    }

    public async Task<CurrencyConversionDto?> GetCurrencyConversionById(int id)
    {
        try
        {
            var conversion = await _dbContext.CurrencyConversions.FirstOrDefaultAsync(c => c.IdCurrencyConversion == id);
            return conversion == null ? null : _mapper.Map<CurrencyConversionDto>(conversion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency conversion with ID: {Id}", id);
            throw;
        }
    }

    public async Task<CurrencyConversionDto?> AddCurrencyConversion(CurrencyConversionDto dto)
    {
        try
        {
            var entity = _mapper.Map<CurrencyConversion>(dto);
            var addedEntity = await _dbContext.CurrencyConversions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<CurrencyConversionDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding currency conversion: {@CurrencyConversionDto}", dto);
            return null;
        }
    }

    public async Task<CurrencyConversionDto?> UpdateCurrencyConversion(CurrencyConversionDto dto)
    {
        try
        {
            var conversion = await _dbContext.CurrencyConversions.FirstOrDefaultAsync(c => c.IdCurrencyConversion == dto.IdCurrencyConversion);

            if (conversion == null) return null;

            conversion.FromCurrency = dto.FromCurrency;
            conversion.ToCurrency = dto.ToCurrency;
            conversion.RateDate = dto.RateDate;
            conversion.ConversionRate = dto.ConversionRate;

            var updatedEntity = _dbContext.CurrencyConversions.Update(conversion);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<CurrencyConversionDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency conversion with ID: {Id}", dto.IdCurrencyConversion);
            return null;
        }
    }
}
