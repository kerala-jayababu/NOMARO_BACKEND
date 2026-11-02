using Asp.Versioning;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>Birthdays, work anniversaries and the holiday calendar (Self Portal dashboard and Payroll).</summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeCalendarController : ControllerBase
    {
        private readonly IEmployeeCalendarService _employeeCalendarService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public EmployeeCalendarController(
            IEmployeeCalendarService employeeCalendarService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService)
        {
            _employeeCalendarService = employeeCalendarService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        /// <summary>Today's and upcoming birthdays and work anniversaries (any logged-in employee).</summary>
        /// <param name="upcomingDays">Days after today counted as upcoming (default 7).</param>
        [HttpGet("GetCelebrations")]
        public async Task<IActionResult> GetCelebrations(int upcomingDays = 7)
        {
            try
            {
                var result = await _employeeCalendarService.GetCelebrations(upcomingDays);
                return Ok(ApiResponseDto<CelebrationsDto>.CreateSuccess(result, "Celebrations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>Sends the birthday (BIRTHDAYMESSAGE) or work anniversary (WORKDAYMESSAGE) email. Needs Add permission on the Celebrations screen.</summary>
        [HttpPost("SendCongratulationsEmail")]
        public async Task<IActionResult> SendCongratulationsEmail([FromBody] SendCongratulationsDto dto)
        {
            if (dto == null || dto.IdEmployee <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Select an employee."));

            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            var screenCode = _configuration["ScreenCodes:Celebrations"];
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(idEmployee), screenCode, "A");
            if (!hasPermission)
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            try
            {
                var sentAt = await _employeeCalendarService.SendCongratulationsEmail(dto, int.Parse(idEmployee));
                return Ok(ApiResponseDto<DateTime>.CreateSuccess(sentAt, "Congratulations email sent successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>Holidays between two dates (inclusive), for the Holiday Calendar (any logged-in employee).</summary>
        [HttpGet("GetHolidayCalendar")]
        public async Task<IActionResult> GetHolidayCalendar(DateTime fromDate, DateTime toDate)
        {
            if (fromDate == default || toDate == default || toDate < fromDate)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Enter a valid date range."));
            if ((toDate - fromDate).TotalDays > 400)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Date range must not exceed one year."));

            try
            {
                var holidays = await _employeeCalendarService.GetHolidayCalendar(fromDate, toDate);
                return Ok(ApiResponseDto<IEnumerable<HolidayCalendarDto>>.CreateSuccess(holidays, "Holidays retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
