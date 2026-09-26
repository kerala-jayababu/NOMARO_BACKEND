using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Services.Interface.Time___Attendance.Shift
{
    public interface ILeaveService
    {
        Task<IEnumerable<EmployeeLeaveReportDto>> GetEmployeeLeaveReport(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<IEnumerable<EmployeeUnauthorizedAbsenceDto>> GetUnauthorizedAbsences(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null);
    }
}

