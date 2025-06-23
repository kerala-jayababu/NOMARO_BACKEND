using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Shift
{
    public interface IShiftService
    {
        Task<IEnumerable<ShiftDto>> GetShiftList();
        Task<ShiftDto?> GetShiftById(int id);
        Task<ShiftDto?> AddShift(ShiftDto shift);
        Task<ShiftDto?> UpdateShift(ShiftDto shift);
        Task<List<ClockInOutDto>> GetClockInClockOutDetailsAsync(string idEmployeeString, DateTime dateFrom, DateTime dateTo);
        Task<IEnumerable<DayAttendanceDto>> GetDayAttendanceDetails(
            DateTime dateFrom, DateTime dateTo , List<int> idEmployees, int? idDepartment = null);
        Task<bool> ApproveTimesheetAsync(List<ApproveTimesheetDto> dtos, int employeeId);
        Task<bool> UpdateClockInOutMissingEntriesAsync(List<UpdateClockInOutMissingEntryDto> dtos);

        Task<bool> UpdateAttendanceShortTimeDetailsAsync(UpdateShortTimeReasonDto dto);

    }
}

