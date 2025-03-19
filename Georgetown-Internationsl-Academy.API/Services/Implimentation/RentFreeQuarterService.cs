using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RentFreeQuarterService : IRentFreeQuarterService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<RentFreeQuarterService> _logger;

    public RentFreeQuarterService(ApplicationDBContext dbContext, IMapper mapper, ILogger<RentFreeQuarterService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<RentFreeQuarterDto>> GetRentFreeQuarters()
    {
        try
        {
            var entities = await _dbContext.RentFreeQuarters.ToListAsync();
            return _mapper.Map<IEnumerable<RentFreeQuarterDto>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rent-free quarters.");
            throw;
        }
    }

    public async Task<IEnumerable<RentFreeQuarterDurationsDto>> GetRentFreeQuarterDurations()
    {
        try
        {
            var entities = await _dbContext.RentFreeQuarterDurations.ToListAsync();
            return _mapper.Map<IEnumerable<RentFreeQuarterDurationsDto>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rent-free quarters.");
            throw;
        }
    }

    public async Task<RentFreeQuarterDto?> GetRentFreeQuarterById(int id)
    {
        try
        {
            var entity = await _dbContext.RentFreeQuarters.FirstOrDefaultAsync(x => x.IdRentFreeQuater == id);
            if (entity == null)
            {
                _logger.LogWarning("Rent-free quarter with ID {Id} not found.", id);
                return null;
            }
            return _mapper.Map<RentFreeQuarterDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rent-free quarter with ID {Id}.", id);
            throw;
        }
    }

    public async Task<RentFreeQuarterDto?> AddRentFreeQuarter(RentFreeQuarterDto dto)
    {
        try
        {           

            var templateEntity = new RentFreeQuarter();
            templateEntity.IdEmployee = dto.IdEmployee;
            templateEntity.IdRentFreeQuater = null;
            templateEntity.IdRentFreeQuarterEnum = dto.IdRentFreeQuarterEnum;
            templateEntity.TotalAnnualRent = dto.TotalAnnualRent;
            templateEntity.DurationInMonths = dto.DurationInMonths;
            templateEntity.ValidFrom = dto.ValidFrom;
            templateEntity.ValidTo = dto.ValidTo;
            templateEntity.PeriodText = dto.PeriodText;
            templateEntity.MonthlyRent = dto.MonthlyRent;
            templateEntity.TaxRate = dto.TaxRate;
            templateEntity.AnnualTaxAmount = dto.AnnualTaxAmount;
            templateEntity.MonthlyTaxAmount = dto.MonthlyTaxAmount;

            await _dbContext.RentFreeQuarters.AddAsync(templateEntity);
            await _dbContext.SaveChangesAsync();
            var resultDto = new RentFreeQuarterDto
            {
                IdRentFreeQuater = (int)templateEntity.IdRentFreeQuater,
                IdEmployee = templateEntity.IdEmployee,
                IdRentFreeQuarterEnum = templateEntity.IdRentFreeQuarterEnum,
                TotalAnnualRent = templateEntity.TotalAnnualRent,
                DurationInMonths = templateEntity.DurationInMonths,
                ValidFrom = templateEntity.ValidFrom,
                MonthlyRent = templateEntity.MonthlyRent,
                TaxRate = templateEntity.TaxRate,
                AnnualTaxAmount = templateEntity.AnnualTaxAmount,
                MonthlyTaxAmount = templateEntity.MonthlyTaxAmount
            };

            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding rent-free quarter: {@Dto}.", dto);
            return null;
        }
    }

    public async Task<RentFreeQuarterDto?> UpdateRentFreeQuarter(RentFreeQuarterDto dto)
    {
        try
        {
            var entity = await _dbContext.RentFreeQuarters.FirstOrDefaultAsync(x => x.IdRentFreeQuater == dto.IdRentFreeQuater);
            if (entity == null)
            {
                _logger.LogWarning("Rent-free quarter with ID {Id} not found for update.", dto.IdRentFreeQuater);
                return null;
            }

            entity.IdEmployee = dto.IdEmployee;
            entity.IdRentFreeQuarterEnum = dto.IdRentFreeQuarterEnum;
            entity.TotalAnnualRent = dto.TotalAnnualRent;
            entity.DurationInMonths = dto.DurationInMonths;
            entity.ValidFrom = dto.ValidFrom;
            entity.ValidTo = dto.ValidTo;
            entity.PeriodText = dto.PeriodText;
            entity.MonthlyRent = dto.MonthlyRent;
            entity.TaxRate = dto.TaxRate;
            entity.AnnualTaxAmount = dto.AnnualTaxAmount;
            entity.MonthlyTaxAmount = dto.MonthlyTaxAmount;

            _dbContext.RentFreeQuarters.Update(entity);
             await _dbContext.SaveChangesAsync();
            // Manually mapping the updated entity back to DTO
            var updatedDto = new RentFreeQuarterDto
            {
                IdRentFreeQuater = (int)entity.IdRentFreeQuater,
                IdEmployee = entity.IdEmployee,
                TotalAnnualRent = entity.TotalAnnualRent,
                DurationInMonths = entity.DurationInMonths,
                ValidFrom = entity.ValidFrom,
                MonthlyRent = entity.MonthlyRent,
                TaxRate = entity.TaxRate,
                AnnualTaxAmount = entity.AnnualTaxAmount,
                MonthlyTaxAmount = entity.MonthlyTaxAmount
            };

            return updatedDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rent-free quarter with ID {Id}.", dto.IdRentFreeQuater);
            return null;
        }
    }

  
}
