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
    private readonly IAuditService _auditService;

    public CurrencyConversionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<CurrencyConversionService> logger, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<IEnumerable<CurrencyConversionDto>> GetAllCurrencyConversions()
    {
        try
        {
            var conversions = await _dbContext.CurrencyConversions .OrderByDescending(x=>x.RateDate).ToListAsync();
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
            _logger.LogInformation("Attempting to insert CurrencyConversion: {@CurrencyConversionDto}", dto);
            var entity = new CurrencyConversion
            {
                FromCurrency = dto.FromCurrency,
                ToCurrency = dto.ToCurrency,
                RateDate = dto.RateDate,
                ConversionRate = dto.ConversionRate
            };
            _logger.LogInformation("Mapped CurrencyConversion entity before insert: {@CurrencyConversion}", entity);
            var addedEntity = await _dbContext.CurrencyConversions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAuditAsync(
                actionType: "Create",
                entityName: "CurrencyConversion",
                entityId: addedEntity.Entity.IdCurrencyConversion,
                actionDetails: new { after = addedEntity.Entity });

            // Manual mapping from entity to DTO
            var resultDto = new CurrencyConversionDto
            {
                IdCurrencyConversion = addedEntity.Entity.IdCurrencyConversion,
                FromCurrency = addedEntity.Entity.FromCurrency,
                ToCurrency = addedEntity.Entity.ToCurrency,
                RateDate = addedEntity.Entity.RateDate,
                ConversionRate = addedEntity.Entity.ConversionRate
            };
            _logger.LogInformation("Returning CurrencyConversionDto response: {@ResponseCurrencyConversionDto}", resultDto);

            return resultDto;
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

            await _auditService.LogAuditAsync(
                actionType: "Update",
                entityName: "CurrencyConversion",
                entityId: updatedEntity.Entity.IdCurrencyConversion,
                actionDetails: new { after = updatedEntity.Entity });

            return _mapper.Map<CurrencyConversionDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency conversion with ID: {Id}", dto.IdCurrencyConversion);
            return null;
        }
    }
}
