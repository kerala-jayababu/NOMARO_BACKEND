using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// HR: send policy / agreement documents to employees for electronic acceptance. Screen EMPAGREE (V / A / U).
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeAgreementController : ControllerBase
    {
        private readonly IEmployeeAgreementService _service;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IConfiguration _configuration;

        public EmployeeAgreementController(IEmployeeAgreementService service, IRoleBasedScreenService roleBasedService, IConfiguration configuration)
        {
            _service = service;
            _roleBasedService = roleBasedService;
            _configuration = configuration;
        }

        internal static ClientInfo ClientOf(HttpContext ctx)
        {
            var ip = ctx.Request.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrWhiteSpace(ip)) ip = ctx.Connection.RemoteIpAddress?.ToString();
            if (ip != null && ip.Contains(',')) ip = ip.Split(',')[0].Trim();
            return new ClientInfo(ip, ctx.Request.Headers["User-Agent"].ToString());
        }

        private async Task<(int? me, IActionResult? denied)> Check(string action)
        {
            if (!int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var me))
                return (null, Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found.")));
            // the Employee Agreements screen, or the Agreements tab of Employee Profile
            var ok = await _roleBasedService.CheckEmployeePermission(me, (_configuration["ScreenCodes:EmployeeAgreements"] ?? "EMPAGREE"), action)
                     || await _roleBasedService.CheckEmployeePermission(me, (_configuration["ScreenCodes:EmployeeProfile"] ?? "EMPPROF"), action);
            return ok ? (me, null) : (null, StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action.")));
        }

        private IActionResult Fail(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"))
        };

        [HttpGet("GetEmployees")]
        public async Task<IActionResult> GetEmployees()
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            try { return Ok(ApiResponseDto<List<AgreementEmployeeOptionDto>>.CreateSuccess(await _service.GetEmployees(), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetAgreements")]
        public async Task<IActionResult> GetAgreements([FromQuery] EmployeeAgreementFilterDto filter)
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            try { return Ok(ApiResponseDto<List<EmployeeAgreementListDto>>.CreateSuccess(await _service.GetAgreements(filter ?? new()), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("GetAgreementById")]
        public async Task<IActionResult> GetAgreementById(int id)
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            try
            {
                var dto = await _service.GetAgreement(id);
                return dto == null ? NotFound(ApiResponseDto<string>.CreateFailure("Agreement not found.")) : Ok(ApiResponseDto<EmployeeAgreementDetailDto>.CreateSuccess(dto, "OK"));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SendAgreement")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> SendAgreement([FromForm] SendEmployeeAgreementDto dto)
        {
            var (me, denied) = await Check("A");
            if (denied != null) return denied;
            try
            {
                var id = await _service.SendAgreement(dto, me!.Value, ClientOf(HttpContext));
                return Ok(ApiResponseDto<int>.CreateSuccess(id, "Agreement sent to the employee."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("ResendAgreement")]
        public async Task<IActionResult> ResendAgreement(int id)
        {
            var (me, denied) = await Check("U");
            if (denied != null) return denied;
            try
            {
                await _service.ResendAgreement(id, me!.Value, ClientOf(HttpContext));
                return Ok(ApiResponseDto<string>.CreateSuccess("Sent", "A new link has been emailed. Earlier links no longer work."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("CancelAgreement")]
        public async Task<IActionResult> CancelAgreement(int id, string? reason)
        {
            var (me, denied) = await Check("U");
            if (denied != null) return denied;
            try
            {
                await _service.CancelAgreement(id, reason, me!.Value, ClientOf(HttpContext));
                return Ok(ApiResponseDto<string>.CreateSuccess("Cancelled", "Agreement cancelled. The link no longer works."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("DownloadAgreement")]
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            var file = await _service.GetAgreementFile(id);
            return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("File not found.")) : File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("DownloadCertificate")]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            var file = await _service.GetCertificateFile(id);
            return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("Certificate not found.")) : File(file.Content, file.ContentType, file.FileName);
        }
    }

    /// <summary>
    /// Public review page opened from the email link. No login: the random link token identifies the agreement,
    /// and a one-time code emailed to the employee confirms who is responding.
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [AllowAnonymous]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class AgreementPortalController : ControllerBase
    {
        private readonly IEmployeeAgreementService _service;

        public AgreementPortalController(IEmployeeAgreementService service)
        {
            _service = service;
        }

        private IActionResult Fail(Exception ex) => ex is InvalidOperationException
            ? BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message))
            : StatusCode(500, ApiResponseDto<string>.CreateFailure("Something went wrong. Please try again."));

        [HttpPost("GetAgreement")]
        public async Task<IActionResult> GetAgreement([FromBody] AgreementTokenDto dto)
        {
            try { return Ok(ApiResponseDto<AgreementPortalViewDto>.CreateSuccess(await _service.GetPortalView(dto?.Token ?? "", EmployeeAgreementController.ClientOf(HttpContext)), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        // POST so that email security scanners following links never register a view
        [HttpPost("GetDocument")]
        public async Task<IActionResult> GetDocument([FromBody] AgreementTokenDto dto)
        {
            try
            {
                var file = await _service.GetPortalDocument(dto?.Token ?? "", EmployeeAgreementController.ClientOf(HttpContext));
                return file == null ? NotFound(ApiResponseDto<string>.CreateFailure("Document not available.")) : File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] AgreementTokenDto dto)
        {
            try
            {
                var message = await _service.SendOtp(dto?.Token ?? "", EmployeeAgreementController.ClientOf(HttpContext));
                return Ok(ApiResponseDto<string>.CreateSuccess(message, message));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("Respond")]
        public async Task<IActionResult> Respond([FromBody] AgreementResponseDto dto)
        {
            try
            {
                var view = await _service.Respond(dto, EmployeeAgreementController.ClientOf(HttpContext));
                return Ok(ApiResponseDto<AgreementPortalViewDto>.CreateSuccess(view, view.State == "ACCEPTED" ? "Thank you. Your acceptance has been recorded." : "Your response has been recorded."));
            }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
