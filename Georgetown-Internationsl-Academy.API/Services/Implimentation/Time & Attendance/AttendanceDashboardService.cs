using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance
{
    public class AttendanceDashboardService : IAttendanceDashboardService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<AttendanceDashboardService> _logger;

        public AttendanceDashboardService(ApplicationDBContext dbContext, ILogger<AttendanceDashboardService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<LiveDashboardAttendanceDto?> GetLiveDashboardAttendanceAsync(DateTime attendanceDate, int? idDepartment)
        {
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();

                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                var parameters = new DynamicParameters();
                parameters.Add("@AttendanceDate", attendanceDate, DbType.Date);
                parameters.Add("@IdDepartment", idDepartment ?? 0, DbType.Int32);

                var result = await connection.QueryAsync<LiveDashboardAttendanceDto>(
                    "LiveDashboard_Attendance",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch live dashboard attendance for {AttendanceDate}", attendanceDate);
                throw;
            }
        }

        public async Task<IEnumerable<LiveDashboardDetailDto>> GetLiveDashboardDetailsAsync(DateTime attendanceDate, string detailType, int? idDepartment)
        {
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();

                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                var parameters = new DynamicParameters();
                parameters.Add("@AttendanceDate", attendanceDate, DbType.Date);
                parameters.Add("@DetailType", detailType, DbType.String);
                parameters.Add("@IdDepartment", idDepartment ?? 0, DbType.Int32);

                var result = await connection.QueryAsync<LiveDashboardDetailDto>(
                    "GetLiveDashboardDetailData",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result ?? Enumerable.Empty<LiveDashboardDetailDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to fetch live dashboard details for {AttendanceDate} and {DetailType}",
                    attendanceDate,
                    detailType);
                throw;
            }
        }
    }
}

