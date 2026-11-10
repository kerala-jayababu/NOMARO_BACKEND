using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// Document Management (organisation / office documents).
    /// Screens: Document Register (GENDOCREG V/A/U/D), Document Upload (GENDOCUPLOAD V/A), Expiry Monitoring (GENDOCEXPIRY V),
    /// General Document Types (GENDOCTYPE V/A/U), General Document Subtypes (GENDOCSUBTYPE V/A/U).
    ///   Register V or Upload V or Expiry V : view, search, download
    ///   Register A or Upload A             : register a document, upload files
    ///   Register U                         : edit document / record details, activate / deactivate
    ///   Register D                         : mark records incorrect / superseded, access to all offices
    ///   Document Reports V (GENDOCREPORT)  : reports and Excel export
    ///   Document Ack Dashboard (GENDOCACK) : V = dashboard and export, A = send reminder emails
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class GeneralDocumentController : ControllerBase
    {
        private readonly IGeneralDocumentService _service;
        private readonly IConfiguration _configuration;

        public GeneralDocumentController(IGeneralDocumentService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        private string Code(string key, string fallback) => _configuration[$"ScreenCodes:{key}"] ?? fallback;
        private string RegisterCode => Code("DocumentRegister", "GENDOCREG");
        private string UploadCode => Code("DocumentUpload", "GENDOCUPLOAD");
        private string ExpiryCode => Code("DocumentExpiry", "GENDOCEXPIRY");
        private string ReportCode => Code("DocumentReports", "GENDOCREPORT");
        private string AckCode => Code("DocumentAckDashboard", "GENDOCACK");
        private string TypeCode => Code("GeneralDocumentTypes", "GENDOCTYPE");
        private string SubtypeCode => Code("GeneralDocumentSubtypes", "GENDOCSUBTYPE");

        private static ObjectResult Forbidden =>
            new(ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action.")) { StatusCode = 403 };

        private int? Me => int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private async Task<GeneralDocumentUserContext?> UserContext()
        {
            if (Me is not int me) return null;
            var p = await _service.GetScreenPermissions(me, RegisterCode, UploadCode, ExpiryCode, ReportCode, AckCode);
            bool Has(string code, char action) => p.TryGetValue(code, out var s) && s.Contains(action);
            return new GeneralDocumentUserContext
            {
                IdEmployee = me,
                CanView = Has(RegisterCode, 'V') || Has(UploadCode, 'V') || Has(ExpiryCode, 'V'),
                CanRegister = Has(RegisterCode, 'A') || Has(UploadCode, 'A'),
                CanUpload = Has(RegisterCode, 'A') || Has(UploadCode, 'A'),
                CanEdit = Has(RegisterCode, 'U'),
                CanMarkStatus = Has(RegisterCode, 'D'),
                CanReport = Has(ReportCode, 'V'),
                CanViewAck = Has(AckCode, 'V'),
                CanSendAckReminder = Has(AckCode, 'A'),
                AllOfficesPermission = Has(RegisterCode, 'D'),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
        }

        /// <summary>Runs the action when the caller has the permission picked by <paramref name="allowed"/>.</summary>
        private async Task<IActionResult> Run(Func<GeneralDocumentUserContext, bool> allowed, Func<GeneralDocumentUserContext, Task<IActionResult>> action)
        {
            var user = await UserContext();
            if (user == null) return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            if (!allowed(user)) return Forbidden;
            try { return await action(user); }
            catch (Exception ex) { return Fail(ex); }
        }

        private async Task<IActionResult> RunMaster(string screenCode, char action, Func<int, Task<IActionResult>> body)
        {
            if (Me is not int me) return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            var p = await _service.GetScreenPermissions(me, screenCode);
            if (!(p.TryGetValue(screenCode, out var s) && s.Contains(action))) return Forbidden;
            try { return await body(me); }
            catch (Exception ex) { return Fail(ex); }
        }

        private IActionResult Fail(Exception ex) => ex switch
        {
            UnauthorizedAccessException => Forbidden,
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"))
        };

        #region Masters

        [HttpGet("GetTypes")]
        public Task<IActionResult> GetTypes(bool includeInactive = true) =>
            RunMaster(TypeCode, 'V', async _ => Ok(ApiResponseDto<List<GeneralDocumentTypeDto>>.CreateSuccess(await _service.GetTypes(includeInactive), "OK")));

        [HttpPost("SaveType")]
        public Task<IActionResult> SaveType([FromBody] GeneralDocumentTypeDto dto) =>
            RunMaster(TypeCode, dto?.IdGeneralDocumentType > 0 ? 'U' : 'A', async me =>
                Ok(ApiResponseDto<int>.CreateSuccess(await _service.SaveType(dto!, me), dto!.IdGeneralDocumentType > 0 ? "Document type updated." : "Document type added.")));

        [HttpGet("GetSubtypes")]
        public Task<IActionResult> GetSubtypes(int? idGeneralDocumentType, bool includeInactive = true) =>
            RunMaster(SubtypeCode, 'V', async _ => Ok(ApiResponseDto<List<GeneralDocumentSubtypeDto>>.CreateSuccess(await _service.GetSubtypes(idGeneralDocumentType, includeInactive), "OK")));

        // Document types for the Subtypes screen (only needs Subtypes view permission)
        [HttpGet("GetTypeOptions")]
        public Task<IActionResult> GetTypeOptions() =>
            RunMaster(SubtypeCode, 'V', async _ => Ok(ApiResponseDto<List<GeneralDocumentTypeDto>>.CreateSuccess(await _service.GetTypes(true), "OK")));

        [HttpPost("SaveSubtype")]
        public Task<IActionResult> SaveSubtype([FromBody] GeneralDocumentSubtypeDto dto) =>
            RunMaster(SubtypeCode, dto?.IdGeneralDocumentSubtype > 0 ? 'U' : 'A', async me =>
                Ok(ApiResponseDto<int>.CreateSuccess(await _service.SaveSubtype(dto!, me), dto!.IdGeneralDocumentSubtype > 0 ? "Subtype updated." : "Subtype added.")));

        #endregion

        #region Documents

        [HttpGet("GetLookups")]
        public Task<IActionResult> GetLookups() =>
            Run(u => u.CanView || u.CanUpload || u.CanReport || u.CanViewAck, async u => Ok(ApiResponseDto<GeneralDocumentLookupsDto>.CreateSuccess(await _service.GetLookups(u), "OK")));

        [HttpPost("GetDocuments")]
        public Task<IActionResult> GetDocuments([FromBody] GeneralDocumentFilterDto? filter) =>
            Run(u => u.CanView || u.CanUpload, async u =>
                Ok(ApiResponseDto<List<GeneralDocumentListDto>>.CreateSuccess(await _service.GetDocuments(filter ?? new(), u), "Documents retrieved successfully.")));

        [HttpPost("GetSummary")]
        public Task<IActionResult> GetSummary([FromBody] GeneralDocumentFilterDto? filter) =>
            Run(u => u.CanView, async u => Ok(ApiResponseDto<GeneralDocumentSummaryDto>.CreateSuccess(await _service.GetSummary(filter ?? new(), u), "OK")));

        [HttpGet("GetDocument")]
        public Task<IActionResult> GetDocument(int id) =>
            Run(u => u.CanView || u.CanUpload, async u => Ok(ApiResponseDto<GeneralDocumentDetailDto>.CreateSuccess(await _service.GetDocument(id, u), "OK")));

        [HttpPost("SaveDocument")]
        public Task<IActionResult> SaveDocument([FromBody] SaveGeneralDocumentDto dto) =>
            Run(u => dto?.IdGeneralDocument > 0 ? u.CanEdit : u.CanRegister, async u =>
            {
                var id = await _service.SaveDocument(dto!, u);
                return Ok(ApiResponseDto<int>.CreateSuccess(id, dto!.IdGeneralDocument > 0 ? "Document updated." : "Document registered."));
            });

        [HttpPost("SetDocumentActive")]
        public Task<IActionResult> SetDocumentActive([FromBody] GeneralDocumentActiveDto dto) =>
            Run(u => u.CanEdit, async u =>
            {
                await _service.SetDocumentActive(dto?.IdGeneralDocument ?? 0, dto?.IsActive ?? false, dto?.Remarks, u);
                return Ok(ApiResponseDto<string>.CreateSuccess("OK", dto!.IsActive ? "Document activated." : "Document deactivated."));
            });

        #endregion

        #region Records and files

        [HttpPost("UploadRecord")]
        [RequestSizeLimit(110 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
        public Task<IActionResult> UploadRecord([FromForm] UploadGeneralDocumentRecordDto dto) =>
            Run(u => u.CanUpload, async u => Ok(ApiResponseDto<int>.CreateSuccess(await _service.UploadRecord(dto, u), "Document uploaded.")));

        [HttpPost("UpdateRecord")]
        public Task<IActionResult> UpdateRecord([FromBody] UpdateGeneralDocumentRecordDto dto) =>
            Run(u => u.CanEdit, async u =>
            {
                await _service.UpdateRecord(dto, u);
                return Ok(ApiResponseDto<string>.CreateSuccess("OK", "Record details updated."));
            });

        [HttpPost("SetRecordStatus")]
        public Task<IActionResult> SetRecordStatus([FromBody] GeneralDocumentRecordStatusDto dto) =>
            Run(u => u.CanMarkStatus, async u =>
            {
                await _service.SetRecordStatus(dto, u);
                return Ok(ApiResponseDto<string>.CreateSuccess("OK", $"Record marked {dto?.Status?.ToLowerInvariant()}."));
            });

        /// <summary>inline=true is logged as VIEW (preview), otherwise DOWNLOAD.</summary>
        [HttpGet("DownloadFile")]
        public Task<IActionResult> DownloadFile(int id, bool inline = false) =>
            Run(u => u.CanView || u.CanUpload, async u =>
            {
                var file = await _service.GetFile(id, inline, u);
                return File(file.Content, file.ContentType, file.FileName);
            });

        [HttpPost("DownloadZip")]
        public Task<IActionResult> DownloadZip([FromBody] GeneralDocumentZipRequestDto request) =>
            Run(u => u.CanView, async u =>
            {
                var file = await _service.GetZip(request, u);
                return File(file.Content, file.ContentType, file.FileName);
            });

        #endregion

        #region Reports

        [HttpGet("GetReportList")]
        public Task<IActionResult> GetReportList() =>
            Run(u => u.CanReport, _ => Task.FromResult<IActionResult>(Ok(ApiResponseDto<object>.CreateSuccess(
                Services.Implimentation.GeneralDocumentService.Reports.Select(r => new { code = r.Code, name = r.Name }), "OK"))));

        [HttpPost("GetReport")]
        public Task<IActionResult> GetReport([FromBody] GeneralDocumentReportFilterDto filter) =>
            Run(u => u.CanReport, async u => Ok(ApiResponseDto<GeneralDocumentReportDto>.CreateSuccess(await _service.GetReport(filter, u), "OK")));

        [HttpPost("ExportReport")]
        public Task<IActionResult> ExportReport([FromBody] GeneralDocumentReportFilterDto filter) =>
            Run(u => u.CanReport, async u =>
            {
                var file = await _service.ExportReport(filter, u);
                return File(file.Content, file.ContentType, file.FileName);
            });

        #endregion

        #region Acknowledgement dashboard

        [HttpGet("GetAckDashboard")]
        public Task<IActionResult> GetAckDashboard(int? idGeneralDocumentType) =>
            Run(u => u.CanViewAck, async u => Ok(ApiResponseDto<AckDashboardDto>.CreateSuccess(await _service.GetAckDashboard(idGeneralDocumentType, u), "OK")));

        [HttpGet("GetAckEmployees")]
        public Task<IActionResult> GetAckEmployees(int idGeneralDocument) =>
            Run(u => u.CanViewAck, async u => Ok(ApiResponseDto<List<AckEmployeeDto>>.CreateSuccess(await _service.GetAckEmployees(idGeneralDocument, u), "OK")));

        /// <summary>status: NOT_ACKNOWLEDGED (default) | ACKNOWLEDGED | ALL</summary>
        [HttpGet("ExportAckEmployees")]
        public Task<IActionResult> ExportAckEmployees(int idGeneralDocument, string? status) =>
            Run(u => u.CanViewAck, async u =>
            {
                var file = await _service.ExportAckEmployees(idGeneralDocument, status, u);
                return File(file.Content, file.ContentType, file.FileName);
            });

        [HttpPost("SendAckReminders")]
        public Task<IActionResult> SendAckReminders([FromBody] AckReminderRequestDto request) =>
            Run(u => u.CanSendAckReminder, async u =>
            {
                var r = await _service.SendAckReminders(request ?? new AckReminderRequestDto(), u);
                var msg = $"Reminder sent to {r.Sent} employee{(r.Sent == 1 ? "" : "s")}."
                          + (r.Failed > 0 ? $" {r.Failed} could not be sent." : "")
                          + (r.NoEmail > 0 ? $" {r.NoEmail} have no email address." : "");
                return Ok(ApiResponseDto<AckReminderResultDto>.CreateSuccess(r, msg));
            });

        #endregion
    }
}
