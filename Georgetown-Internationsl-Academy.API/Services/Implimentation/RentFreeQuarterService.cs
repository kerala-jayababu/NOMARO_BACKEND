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
            var entity = _mapper.Map<RentFreeQuarter>(dto);
            var addedEntity = await _dbContext.RentFreeQuarters.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<RentFreeQuarterDto>(addedEntity.Entity);
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

            _mapper.Map(dto, entity);
            _dbContext.RentFreeQuarters.Update(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<RentFreeQuarterDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rent-free quarter with ID {Id}.", dto.IdRentFreeQuater);
            return null;
        }
    }

  
}
