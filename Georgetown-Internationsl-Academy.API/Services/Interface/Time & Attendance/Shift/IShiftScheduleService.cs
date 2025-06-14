using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift
{
    public interface IShiftScheduleService
    {
        Task<IEnumerable<ShiftScheduleDto>> GetAllShiftSchedulesAsync();
        Task<ShiftScheduleDto?> GetShiftScheduleByIdAsync(int id);
        Task<ShiftScheduleDto?> AddShiftScheduleAsync(ShiftScheduleDto dto);
        Task<ShiftScheduleDto?> UpdateShiftScheduleAsync(ShiftScheduleDto dto);
    }
}
