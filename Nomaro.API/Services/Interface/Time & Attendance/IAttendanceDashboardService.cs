using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nomaro.API.DTO.Time___Attendance;

namespace Nomaro.API.Services.Interface.Time___Attendance
{
    public interface IAttendanceDashboardService
    {
        Task<LiveDashboardAttendanceDto?> GetLiveDashboardAttendanceAsync(DateTime attendanceDate, int? idDepartment);
        Task<IEnumerable<LiveDashboardDetailDto>> GetLiveDashboardDetailsAsync(DateTime attendanceDate, string detailType, int? idDepartment);
    }
}


