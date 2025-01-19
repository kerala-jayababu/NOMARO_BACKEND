using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class VacationModeService : IVacationModeService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<VacationModeService> _logger;

    public VacationModeService(ApplicationDBContext dbContext, IMapper mapper, ILogger<VacationModeService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<VacationModeDto>> GetAllVacationModes()
    {
        try
        {
            var vacationModes = await _dbContext.VacationModes.ToListAsync();
            return _mapper.Map<IEnumerable<VacationModeDto>>(vacationModes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vacation modes.");
            throw;
        }
    }

    public async Task<VacationModeDto?> GetVacationModeById(int id)
    {
        try
        {
            var vacationMode = await _dbContext.VacationModes.FirstOrDefaultAsync(vm => vm.IdVacationMode == id);
            return vacationMode == null ? null : _mapper.Map<VacationModeDto>(vacationMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vacation mode with ID: {Id}", id);
            throw;
        }
    }

    public async Task<VacationModeDto?> AddVacationMode(VacationModeDto vacationModeDto)
    {
        try
        {
            var vacationModeEntity = _mapper.Map<VacationMode>(vacationModeDto);
            var addedEntity = await _dbContext.VacationModes.AddAsync(vacationModeEntity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<VacationModeDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vacation mode: {@VacationModeDto}", vacationModeDto);
            return null;
        }
    }

    public async Task<VacationModeDto?> UpdateVacationMode(VacationModeDto vacationModeDto)
    {
        try
        {
            var vacationMode = await _dbContext.VacationModes.FirstOrDefaultAsync(vm => vm.IdVacationMode == vacationModeDto.IdVacationMode);

            if (vacationMode == null)
            {
                _logger.LogWarning("Vacation mode with ID {Id} not found.", vacationModeDto.IdVacationMode);
                return null;
            }

            vacationMode.IdEmployee = vacationModeDto.IdEmployee;
            vacationMode.VacationFrom = vacationModeDto.VacationFrom;
            vacationMode.VacationTo = vacationModeDto.VacationTo;
            vacationMode.IdSubstitueEmployee = vacationModeDto.IdSubstitueEmployee;

            var updatedEntity = _dbContext.VacationModes.Update(vacationMode);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<VacationModeDto>(updatedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vacation mode with ID: {Id}", vacationModeDto.IdVacationMode);
            return null;
        }
    }
}
