using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// HR side of employee profile change requests: review, approve / reject / clarify, settings.
    /// Approvers see the requests waiting for them; "V" on PROFCHGREQ lets a user see all requests.
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class ProfileChangeRequestController : ControllerBase
    {
        private readonly IProfileChangeRequestService _service;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IConfiguration _configuration;

        public ProfileChangeRequestController(IProfileChangeRequestService service, IRoleBasedScreenService roleBasedService, IConfiguration configuration)
        {
            _service = service;
            _roleBasedService = roleBasedService;
            _configuration = configuration;
        }

        private int? CurrentEmployeeId =>
            int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        // Screen codes come from appsettings ScreenCodes; the defaults are used when a server's appsettings does not have them
        private static readonly Dictionary<string, string> DefaultScreenCodes = new()
        {
            ["ProfileChangeRequests"] = "PROFCHGREQ",
            ["ProfileChangeSettings"] = "PROFCHGSET"
        };

        // What the last failed check looked for, shown in the 403 message so a wrong setup is easy to spot
        private string? _deniedDetail;

        private async Task<bool> Can(int idEmployee, string screenKey, string action)
        {
            var code = _configuration[$"ScreenCodes:{screenKey}"];
            if (string.IsNullOrWhiteSpace(code)) DefaultScreenCodes.TryGetValue(screenKey, out code);
            var ok = await _roleBasedService.CheckEmployeePermission(idEmployee, code!, action);
            if (!ok) _deniedDetail = $"Screen {code}, permission {action}, employee {idEmployee}";
            return ok;
        }

        private IActionResult NoEmployee() => Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
        private IActionResult Forbidden() => StatusCode(403, ApiResponseDto<string>.CreateFailure(
            "You do not have the required permission to perform this action." + (_deniedDetail == null ? "" : $" ({_deniedDetail})")));

        private IActionResult Fail(Exception ex) => ex switch
        {
            ProfileChangeConflictException => Conflict(ApiResponseDto<string>.CreateFailure(ex.Message, "CONFLICT")),
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure("An error occurred. Please try again."))
        };

        [HttpGet("GetDashboardCounts")]
        public async Task<IActionResult> GetDashboardCounts()
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var all = await Can(me.Value, "ProfileChangeRequests", "V");
                var data = await _service.GetDashboard(me.Value, all);
                return Ok(ApiResponseDto<object>.CreateSuccess(new { counts = data, canViewAll = all }, "Counts retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetRequests")]
        public async Task<IActionResult> GetRequests([FromQuery] ProfileChangeHrFilterDto filter)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var all = await Can(me.Value, "ProfileChangeRequests", "V");
                return Ok(ApiResponseDto<List<ProfileChangeRequestListDto>>.CreateSuccess(await _service.GetHrRequests(me.Value, all, filter), "Requests retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetRequestById")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var all = await Can(me.Value, "ProfileChangeRequests", "V");
                var reveal = await Can(me.Value, "ProfileChangeRequests", "D");
                var detail = await _service.GetRequestDetail(id, me.Value, all, reveal);
                return detail == null
                    ? NotFound(ApiResponseDto<string>.CreateFailure("Request not found."))
                    : Ok(ApiResponseDto<ProfileChangeRequestDetailDto>.CreateSuccess(detail, "Request retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("DownloadRequestDocument")]
        public async Task<IActionResult> DownloadRequestDocument(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var all = await Can(me.Value, "ProfileChangeRequests", "V");
                var file = await _service.GetRequestDocument(id, me.Value, all);
                return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("Document not found.")) : File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        // Full bank / ID number for users with "D" (sensitive data) on PROFCHGREQ. Every reveal is written to the audit log.
        [HttpGet("RevealValue")]
        public async Task<IActionResult> RevealValue(int idDetail, string which = "NEW")
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeRequests", "D")) return Forbidden();
                var all = await Can(me.Value, "ProfileChangeRequests", "V");
                var value = await _service.RevealValue(idDetail, which, me.Value, all);
                return value == null ? NotFound(ApiResponseDto<string>.CreateFailure("Value not found.")) : Ok(ApiResponseDto<string>.CreateSuccess(value, "OK"));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        // APPROVE | REJECT | CLARIFY. The workflow engine checks that the user is the approver at the current level.
        [HttpPost("ActionRequest")]
        public async Task<IActionResult> ActionRequest([FromBody] ProfileChangeActionDto dto)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var message = await _service.ActionRequest(me.Value, dto);
                return Ok(ApiResponseDto<string>.CreateSuccess(message, message));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetEmployeeChangeHistory")]
        public async Task<IActionResult> GetEmployeeChangeHistory(int idEmployee)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeRequests", "V")) return Forbidden();
                return Ok(ApiResponseDto<List<ProfileChangeHistoryEntryDto>>.CreateSuccess(await _service.GetEmployeeChangeHistory(idEmployee), "History retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        #region Settings

        [HttpGet("GetRequestTypes")]
        public async Task<IActionResult> GetRequestTypes()
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeSettings", "V")) return Forbidden();
                var lookups = await _service.GetLookups(activeOnly: false);
                return Ok(ApiResponseDto<List<ProfileChangeRequestTypeDto>>.CreateSuccess(lookups.RequestTypes, "Request types retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("UpdateRequestType")]
        public async Task<IActionResult> UpdateRequestType([FromBody] ProfileChangeRequestTypeUpdateDto dto)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeSettings", "U")) return Forbidden();
                await _service.UpdateRequestType(dto, me.Value);
                return Ok(ApiResponseDto<string>.CreateSuccess("Request type updated.", "Request type updated."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetWorkflows")]
        public async Task<IActionResult> GetWorkflows()
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeSettings", "V")) return Forbidden();
                return Ok(ApiResponseDto<List<ProfileChangeWorkflowDto>>.CreateSuccess(await _service.GetWorkflows(), "Workflows retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetWorkflowLookups")]
        public async Task<IActionResult> GetWorkflowLookups()
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeSettings", "V")) return Forbidden();
                return Ok(ApiResponseDto<ProfileChangeWorkflowLookupsDto>.CreateSuccess(await _service.GetWorkflowLookups(), "Lookups retrieved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SaveWorkflow")]
        public async Task<IActionResult> SaveWorkflow([FromBody] ProfileChangeWorkflowDto dto)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                if (!await Can(me.Value, "ProfileChangeSettings", "U")) return Forbidden();
                await _service.SaveWorkflow(dto);
                return Ok(ApiResponseDto<string>.CreateSuccess("Approval levels saved.", "Approval levels saved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion
    }
}
