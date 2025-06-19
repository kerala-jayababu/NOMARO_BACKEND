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
            DateTime? dateFrom = null, DateTime? dateTo = null, int? idEmployee = null, int? idDepartment = null);

        Task<bool> ApproveTimesheetAsync(ApproveTimesheetDto dto,int EmployeeId);
        Task<bool> UpdateClockInOutMissingEntryAsync(UpdateClockInOutMissingEntryDto dto);
        Task<bool> UpdateAttendanceShortTimeDetailsAsync(UpdateShortTimeReasonDto dto);

    }
}

