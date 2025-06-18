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

        public async Task<IEnumerable<ShiftScheduleDto>> GetAllShiftSchedulesAsync(int idShift)
        {
            try
            {
                var schedules = await (from s in _dbContext.ShiftSchedules
                                       join shift in _dbContext.ShiftDefinitions
                                           on s.IdShift equals shift.IdShift
                                       where s.IdShift == idShift
                                       orderby s.IdShift
                                       select new ShiftScheduleDto
                                       {
                                           IdShiftSchedule = s.IdShiftSchedule,
                                           IdShift = s.IdShift,
                                           StartTime = s.StartTime,
                                           EndTime = s.EndTime,
                                           TotalDurationMinutes = s.TotalDurationMinutes,
                                           TotalDurationHours = s.TotalDurationHours,
                                           WorkDays = s.WorkDays,
                                           ShiftName = shift.ShiftName
                                       }).ToListAsync();

                return schedules;
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

        public async Task<List<ShiftScheduleDto>> ManageShiftSchedulesAsync(List<ShiftScheduleDto> shiftSchedules)
        {
            var resultDtos = new List<ShiftScheduleDto>();

            try
            {
                if (shiftSchedules == null || !shiftSchedules.Any())
                    return resultDtos;
                var idShift = shiftSchedules.First().IdShift;

                var existingSchedules = await _dbContext.ShiftSchedules
                    .Where(s => s.IdShift == idShift)
                    .ToListAsync();
                var incomingScheduleIds = shiftSchedules
          .Where(s => s.IdShiftSchedule.HasValue)
          .Select(s => s.IdShiftSchedule.Value)
          .ToHashSet();

                // Remove schedules not present in incoming list
                var toRemove = existingSchedules
                    .Where(s => !incomingScheduleIds.Contains(s.IdShiftSchedule))
                    .ToList();

                _dbContext.ShiftSchedules.RemoveRange(toRemove);
                foreach (var dto in shiftSchedules)
                {
                    var duration = (dto.EndTime - dto.StartTime).Duration();
                    dto.TotalDurationMinutes = (int)duration.TotalMinutes;
                    dto.TotalDurationHours = (decimal)duration.TotalHours;

                    if (dto.IdShiftSchedule.HasValue && dto.IdShiftSchedule > 0)
                    {
                        // Update
                        var existing = await _dbContext.ShiftSchedules.FindAsync(dto.IdShiftSchedule);
                        if (existing != null)
                        {
                            existing.IdShift = dto.IdShift;
                            existing.StartTime = dto.StartTime;
                            existing.EndTime = dto.EndTime;
                            existing.TotalDurationMinutes = dto.TotalDurationMinutes ?? 0;
                            existing.TotalDurationHours = dto.TotalDurationHours ?? 0;
                            existing.WorkDays = dto.WorkDays;

                            _dbContext.ShiftSchedules.Update(existing);
                            resultDtos.Add(_mapper.Map<ShiftScheduleDto>(existing));
                        }
                    }
                    else
                    {
                        // Add
                        var isDuplicate = await _dbContext.ShiftSchedules.AnyAsync(s =>
                            s.IdShift == dto.IdShift &&
                            s.StartTime == dto.StartTime &&
                            s.EndTime == dto.EndTime);

                        if (!isDuplicate)
                        {
                            var entity = _mapper.Map<ShiftSchedule>(dto);
                            var result = await _dbContext.ShiftSchedules.AddAsync(entity);
                            resultDtos.Add(_mapper.Map<ShiftScheduleDto>(result.Entity));
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                return resultDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing shift schedules.");
                return new List<ShiftScheduleDto>(); // return empty list on failure
            }
        }

    }
}
