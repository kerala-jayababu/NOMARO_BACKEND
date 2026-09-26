using Nomaro.API.DTO;
using Nomaro.API.DTO.Shift;
using Nomaro.API.DTO.Time___Attendance.Shift;
using System.Threading.Tasks;

namespace Nomaro.API.Services.Interface.Shift
{
    public interface IShiftService
    {
        Task<IEnumerable<ShiftDto>> GetShiftList();
        Task<ShiftDto?> GetShiftById(int id);
        Task<ShiftDto?> AddShift(ShiftDto shift);
        Task<ShiftDto?> UpdateShift(ShiftDto shift);
        Task<List<ClockInOutDto>> GetClockInClockOutDetailsAsync(string? idEmployeeString,DateTime dateFrom,DateTime dateTo,int? idDepartment,bool? missingEntryOnly);
        Task<IEnumerable<DayAttendanceDto>> GetDayAttendanceDetails(
            DateTime dateFrom, DateTime dateTo , List<int> idEmployees, int? idDepartment = null);
        Task<bool> ApproveTimesheetAsync(List<ApproveTimesheetDto> dtos, int employeeId);
        Task<bool> UpdateClockInOutMissingEntriesAsync(List<UpdateClockInOutMissingEntryDto> dtos,int employeeId);

        Task<bool> UpdateAttendanceShortTimeDetailsAsync(UpdateShortTimeReasonDto dto);

        Task<List<ClockInOutDetailsDateGroupedDto>> GetClockInClockOutDetailsOfEmployeeGroupedByDate(
                int idEmployee, DateTime dateFrom, DateTime dateTo);
        Task<List<MissingEntryForApprovalDto>> GetMissingEntryDetailsForApproval(int IdLoginnedEmployee, DateTime? dateFrom,
          string? approvalStatus);

        Task<bool> TogglingMissingEntry(int IdClockInDetail);
        Task<bool> ForgotAccessCardMissingEntry(ForgotAccessCardMissingEntryDto entryDetails, int IdLogginedEmployee);
        Task<List<ForgotCardEntryForApprovalDto>> GetForgotCardEntryDetailsForApproval(int IdLoginnedEmployee, DateTime? dateFrom,
         string? approvalStatus);

    }
}


