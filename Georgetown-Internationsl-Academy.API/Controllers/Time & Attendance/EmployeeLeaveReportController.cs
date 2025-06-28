using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Georgetown_Internationsl_Academy.API.DTO;

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


        public EmployeeLeaveReportController(IConfiguration configuration,IRoleBasedScreenService roleBasedScreenService,ILeaveService leaveService)
        {           
            _configuration = configuration;            
            _roleBasedScreenService = roleBasedScreenService;
            _leaveService = leaveService;
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
