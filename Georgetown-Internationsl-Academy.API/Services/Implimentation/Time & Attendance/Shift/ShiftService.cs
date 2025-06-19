using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

public class ShiftService : IShiftService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ShiftDto>> GetShiftList()
    {
        try
        {
            var shifts = await _dbContext.ShiftDefinitions.OrderBy(s => s.ShiftName).ToListAsync();
            return _mapper.Map<IEnumerable<ShiftDto>>(shifts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift list.");
            throw;
        }
    }

    public async Task<ShiftDto?> GetShiftById(int id)
    {
        try
        {
            var shift = await _dbContext.ShiftDefinitions.FirstOrDefaultAsync(s => s.IdShift == id);
            return shift == null ? null : _mapper.Map<ShiftDto>(shift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift by ID {Id}", id);
            throw;
        }
    }

    public async Task<ShiftDto?> AddShift(ShiftDto shiftDto)
    {
        try
        {
            var entity = _mapper.Map<ShiftDefinitionEntity>(shiftDto);
            var result = await _dbContext.ShiftDefinitions.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ShiftDto>(result.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding shift");
            return null;
        }
    }

    public async Task<ShiftDto?> UpdateShift(ShiftDto shiftDto)
    {
        try
        {
            var existing = await _dbContext.ShiftDefinitions.FirstOrDefaultAsync(s => s.IdShift == shiftDto.IdShift);
            if (existing == null)
            {
                _logger.LogWarning("Shift with ID {Id} not found", shiftDto.IdShift);
                return null;
            }

            existing.ShiftName = shiftDto.ShiftName;
            _dbContext.ShiftDefinitions.Update(existing);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ShiftDto>(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift with ID {Id}", shiftDto.IdShift);
            return null;
        }
    }



    public async Task<List<ClockInOutDto>> GetClockInClockOutDetailsAsync(string idEmployeeString, DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IdEmployeeString", idEmployeeString, DbType.String);
                parameters.Add("@Datefrom", dateFrom, DbType.DateTime);
                parameters.Add("@DateTo", dateTo, DbType.DateTime);

                var result = await connection.QueryAsync<ClockInOutDto>(
                    "GetClockInClockOutDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clock-in/out details for Employee(s): {EmployeeString}, From: {DateFrom}, To: {DateTo}",
                idEmployeeString, dateFrom, dateTo);

            throw new Exception("An error occurred while retrieving clock-in/out details. Please try again later.");
        }
    }
}
