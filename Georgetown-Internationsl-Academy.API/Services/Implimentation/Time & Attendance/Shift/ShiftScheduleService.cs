using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Georgetown_International_Academy.API.Services.Implementations.TimeAndAttendance
{
    public class ShiftScheduleService : IShiftScheduleService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ShiftScheduleService> _logger;

        public ShiftScheduleService(ApplicationDBContext dbContext, IMapper mapper, ILogger<ShiftScheduleService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ShiftScheduleDto>> GetAllShiftSchedulesAsync()
        {
            try
            {
                var schedules = await _dbContext.ShiftSchedules.OrderBy(s => s.IdShift).ToListAsync();
                return _mapper.Map<IEnumerable<ShiftScheduleDto>>(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shift schedules list.");
                throw;
            }
        }

        public async Task<ShiftScheduleDto?> GetShiftScheduleByIdAsync(int id)
        {
            try
            {
                var schedule = await _dbContext.ShiftSchedules.FindAsync(id);
                return schedule == null ? null : _mapper.Map<ShiftScheduleDto>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shift schedule with ID {Id}", id);
                throw;
            }
        }

        public async Task<ShiftScheduleDto?> AddShiftScheduleAsync(ShiftScheduleDto dto)
        {
            try
            {
                var duration = (dto.EndTime - dto.StartTime).Duration();
                dto.TotalDurationMinutes = (int)duration.TotalMinutes;
                dto.TotalDurationHours = (decimal)duration.TotalHours;

                var entity = _mapper.Map<ShiftSchedule>(dto);

                var result = await _dbContext.ShiftSchedules.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<ShiftScheduleDto>(result.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding shift schedule.");
                return null;
            }
        }

        public async Task<ShiftScheduleDto?> UpdateShiftScheduleAsync(ShiftScheduleDto dto)
        {
            try
            {
                var existing = await _dbContext.ShiftSchedules.FindAsync(dto.IdShiftSchedule);
                if (existing == null)
                {
                    _logger.LogWarning("Shift schedule with ID {Id} not found", dto.IdShiftSchedule);
                    return null;
                }

                var duration = (dto.EndTime - dto.StartTime).Duration();
                dto.TotalDurationMinutes = (int)duration.TotalMinutes;
                dto.TotalDurationHours = (decimal)duration.TotalHours;

                // Update fields
                existing.IdShift = dto.IdShift;
                existing.StartTime = dto.StartTime;
                existing.EndTime = dto.EndTime;
                existing.TotalDurationMinutes = (int)dto.TotalDurationMinutes;
                existing.TotalDurationHours = (decimal)dto.TotalDurationHours;
                existing.WorkDays = dto.WorkDays;

                _dbContext.ShiftSchedules.Update(existing);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<ShiftScheduleDto>(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shift schedule with ID {Id}", dto.IdShiftSchedule);
                return null;
            }
        }
    }
}
