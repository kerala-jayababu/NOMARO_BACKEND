using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Controllers.Time___Attendance
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]   
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        [HttpPost("MissingEntry")]
        public async Task<IActionResult> MissingEntry(int idEmployee, string mobileNumber, string employeeName, DateTime exitDateTime)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeIdOrZero();

            bool ok = await _smsService.SendSmsMissingEntryAsync(idEmployee, loggedInEmployeeId, mobileNumber, employeeName, exitDateTime);

            if (!ok)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Unable to send SMS (no mobile / no config)."));

            return Ok(ApiResponseDto<string>.CreateSuccess("SMS sent & notification saved."));
        }

        [HttpPost("MissingExit")]
        public async Task<IActionResult> MissingExit(int idEmployee, string mobileNumber, string employeeName, DateTime entryDateTime)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeIdOrZero();

            bool ok = await _smsService.SendSmsMissingExitAsync(idEmployee, loggedInEmployeeId, mobileNumber, employeeName, entryDateTime);

            if (!ok)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Unable to send SMS (no mobile / no config)."));

            return Ok(ApiResponseDto<string>.CreateSuccess("SMS sent & notification saved."));
        }

        [HttpPost("UnauthorizedAbsence")]
        public async Task<IActionResult> UnauthorizedAbsence(int idEmployee, string mobileNumber, string employeeName, DateTime absentDate)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeIdOrZero();

            bool ok = await _smsService.SendSmsUnauthorizedAbsenceAsync(idEmployee, loggedInEmployeeId, mobileNumber, employeeName, absentDate);

            if (!ok)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Unable to send SMS (no mobile / no config)."));

            return Ok(ApiResponseDto<string>.CreateSuccess("SMS sent & notification saved."));
        }

        private int GetLoggedInEmployeeIdOrZero()
        {
            var senderIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(senderIdStr, out int senderId))
                return senderId;

            return 0; // or throw Unauthorized if you want strict
        }
    }
}
