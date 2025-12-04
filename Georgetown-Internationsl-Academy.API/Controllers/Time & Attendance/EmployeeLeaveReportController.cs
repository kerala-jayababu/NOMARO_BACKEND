using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Georgetown_Internationsl_Academy.API.DTO;
using System.Linq;

namespace Georgetown_Internationsl_Academy.API.Controllers.Time___Attendance
{
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeLeaveReportController : ControllerBase
    {
       
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedScreenService;    
        private readonly ILeaveService _leaveService;
        private readonly IAttendanceDashboardService _attendanceDashboardService;


        public EmployeeLeaveReportController(IConfiguration configuration,
                                             IRoleBasedScreenService roleBasedScreenService,
                                             ILeaveService leaveService,
                                             IAttendanceDashboardService attendanceDashboardService)
        {           
            _configuration = configuration;            
            _roleBasedScreenService = roleBasedScreenService;
            _leaveService = leaveService;
            _attendanceDashboardService = attendanceDashboardService;
        }
        [HttpGet("GetEmployeeLeaveReport")]
        public async Task<IActionResult> GetEmployeeLeaveReport(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            if (idEmployee <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("'idEmployee' is required and must be a valid number."));

            try
            {
                var data = await _leaveService.GetEmployeeLeaveReport(idEmployee, dateFrom, dateTo);

                if (data == null || !data.Any())
                    return Ok(ApiResponseDto<IEnumerable<EmployeeLeaveReportDto>>.CreateSuccess(Enumerable.Empty<EmployeeLeaveReportDto>(), "No leave records found."));

                return Ok(ApiResponseDto<IEnumerable<EmployeeLeaveReportDto>>.CreateSuccess(data, "Employee leave report retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLiveDashboardAttendance")]
        public async Task<IActionResult> GetLiveDashboardAttendance(DateTime attendanceDate, int? idDepartment = null)
        {
            if (attendanceDate == default)
                return BadRequest(ApiResponseDto<string>.CreateFailure("'attendanceDate' is required."));

            try
            {
                var effectiveDate = attendanceDate.Date.AddDays(-1);
                var result = await _attendanceDashboardService.GetLiveDashboardAttendanceAsync(attendanceDate, idDepartment);

                if (result == null)
                    return Ok(ApiResponseDto<LiveDashboardAttendanceDto?>.CreateSuccess(null, "No attendance data found for the selected date."));

                return Ok(ApiResponseDto<LiveDashboardAttendanceDto?>.CreateSuccess(result, "Attendance dashboard summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLiveDashboardDetails")]
        public async Task<IActionResult> GetLiveDashboardDetails(DateTime attendanceDate, string detailType, int? idDepartment = null)
        {
            if (attendanceDate == default)
                return BadRequest(ApiResponseDto<string>.CreateFailure("'attendanceDate' is required."));

            if (string.IsNullOrWhiteSpace(detailType))
                return BadRequest(ApiResponseDto<string>.CreateFailure("'detailType' is required."));

            try
            {
                var normalizedDetailType = detailType.Trim();
                var effectiveDate = attendanceDate.Date.AddDays(-1);
                var results = await _attendanceDashboardService.GetLiveDashboardDetailsAsync(attendanceDate, normalizedDetailType, idDepartment);

                if (results == null || !results.Any())
                    return Ok(ApiResponseDto<IEnumerable<LiveDashboardDetailDto>>.CreateSuccess(Enumerable.Empty<LiveDashboardDetailDto>(), "No dashboard details found for the selected criteria."));

                return Ok(ApiResponseDto<IEnumerable<LiveDashboardDetailDto>>.CreateSuccess(results, "Dashboard details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetEmployeeUnauthorizedAbsences")]
        public async Task<IActionResult> GetEmployeeUnauthorizedAbsences(int idEmployee, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            if (idEmployee <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("'idEmployee' is required and must be a valid number."));

            try
            {
                var data = await _leaveService.GetUnauthorizedAbsences(idEmployee, dateFrom, dateTo);

                if (data == null || !data.Any())
                    return Ok(ApiResponseDto<IEnumerable<EmployeeUnauthorizedAbsenceDto>>.CreateSuccess(Enumerable.Empty<EmployeeUnauthorizedAbsenceDto>(), "No unauthorized absences found."));

                return Ok(ApiResponseDto<IEnumerable<EmployeeUnauthorizedAbsenceDto>>.CreateSuccess(data, "Employee unauthorized absences retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



    }
}
