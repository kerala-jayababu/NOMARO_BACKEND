using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift
{
    public interface IShiftScheduleService
    {     
        Task<IEnumerable<ShiftScheduleDto>> GetAllShiftSchedulesAsync(int idShift);
        Task<ShiftScheduleDto?> GetShiftScheduleByIdAsync(int id);
        Task<List<ShiftScheduleDto>> ManageShiftSchedulesAsync(List<ShiftScheduleDto> shiftSchedules);
    }
}
