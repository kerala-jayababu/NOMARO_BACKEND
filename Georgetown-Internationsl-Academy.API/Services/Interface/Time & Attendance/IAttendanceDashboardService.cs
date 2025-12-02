using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance
{
    public interface IAttendanceDashboardService
    {
        Task<LiveDashboardAttendanceDto?> GetLiveDashboardAttendanceAsync(DateTime attendanceDate);
        Task<IEnumerable<LiveDashboardDetailDto>> GetLiveDashboardDetailsAsync(DateTime attendanceDate, string detailType);
    }
}

