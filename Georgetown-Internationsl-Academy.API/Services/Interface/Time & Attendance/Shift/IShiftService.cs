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
    }
}

