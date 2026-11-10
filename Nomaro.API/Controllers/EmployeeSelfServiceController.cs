using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// Employee self-service: the logged-in employee's own profile and change requests.
    /// The employee is always taken from the token, never from a parameter.
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeSelfServiceController : ControllerBase
    {
        private readonly IProfileChangeRequestService _service;

        public EmployeeSelfServiceController(IProfileChangeRequestService service)
        {
            _service = service;
        }

        private int? CurrentEmployeeId =>
            int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private IActionResult NoEmployee() => Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

        private IActionResult Fail(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure("An error occurred. Please try again."))
        };

        [HttpGet("GetMyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var id = CurrentEmployeeId;
            if (id == null) return NoEmployee();
            try
            {
                var profile = await _service.GetMyProfile(id.Value);
                return profile == null
                    ? NotFound(ApiResponseDto<string>.CreateFailure("Employee not found."))
                    : Ok(ApiResponseDto<MyProfileDto>.CreateSuccess(profile, "Profile retrieved successfully."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetLookups")]
        public async Task<IActionResult> GetLookups()
        {
            try
            {
                return Ok(ApiResponseDto<ProfileChangeLookupsDto>.CreateSuccess(await _service.GetLookups(activeOnly: true), "Lookups retrieved successfully."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetMyRequests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var id = CurrentEmployeeId;
            if (id == null) return NoEmployee();
            try
            {
                return Ok(ApiResponseDto<List<ProfileChangeRequestListDto>>.CreateSuccess(await _service.GetMyRequests(id.Value), "Requests retrieved successfully."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetMyRequestById")]
        public async Task<IActionResult> GetMyRequestById(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var detail = await _service.GetRequestDetail(id, me.Value, canViewAll: false, canReveal: false);
                if (detail == null || detail.Employee.IdEmployee != me.Value)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Request not found."));
                return Ok(ApiResponseDto<ProfileChangeRequestDetailDto>.CreateSuccess(detail, "Request retrieved successfully."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SaveRequest")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> SaveRequest([FromForm] SaveProfileChangeRequestDto dto)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var id = await _service.SaveRequest(me.Value, dto);
                return Ok(ApiResponseDto<int>.CreateSuccess(id, "Draft saved."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        // Saves the draft and submits it in one call (the Submit button)
        [HttpPost("SaveAndSubmitRequest")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> SaveAndSubmitRequest([FromForm] SaveProfileChangeRequestDto dto)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var id = await _service.SaveRequest(me.Value, dto);
                try
                {
                    var message = await _service.SubmitRequest(me.Value, id);
                    return Ok(ApiResponseDto<int>.CreateSuccess(id, message));
                }
                catch (InvalidOperationException ex)
                {
                    // Draft is kept; tell the UI which draft so it can continue editing it
                    return BadRequest(new ApiResponseDto<int> { Success = false, Message = ex.Message + " Your changes were saved as a draft.", Data = id });
                }
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SubmitRequest")]
        public async Task<IActionResult> SubmitRequest(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                return Ok(ApiResponseDto<string>.CreateSuccess(await _service.SubmitRequest(me.Value, id)));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("CancelRequest")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                await _service.CancelRequest(me.Value, id);
                return Ok(ApiResponseDto<string>.CreateSuccess("Request cancelled."));
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
                var file = await _service.GetRequestDocument(id, me.Value, canViewAll: false);
                return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("Document not found.")) : File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("DownloadMyDocument")]
        public async Task<IActionResult> DownloadMyDocument(int id)
        {
            var me = CurrentEmployeeId;
            if (me == null) return NoEmployee();
            try
            {
                var file = await _service.GetMyEmployeeDocument(me.Value, id);
                return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("Document not found.")) : File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
