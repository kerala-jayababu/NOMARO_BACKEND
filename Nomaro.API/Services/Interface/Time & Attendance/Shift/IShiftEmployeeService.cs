using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Services.Interface.Time___Attendance.Shift
{
    public interface IShiftEmployeeService
    {
        Task<List<ShiftEmployeeDto>> GetShiftEmployeesByShiftAsync(int idShift);
        Task<List<ShiftEmployeeDto>> ManageShiftEmployeesAsync(List<ShiftEmployeeDto> employees);
    }
}

