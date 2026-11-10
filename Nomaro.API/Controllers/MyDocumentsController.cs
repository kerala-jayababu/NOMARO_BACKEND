using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>Self Portal -> Documents: published documents for the logged-in employee, view and acknowledge.</summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class MyDocumentsController : ControllerBase
    {
        private readonly IGeneralDocumentService _service;

        public MyDocumentsController(IGeneralDocumentService service)
        {
            _service = service;
        }

        private int? Me => int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
        private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string? Agent => Request.Headers.UserAgent.ToString();

        private IActionResult Fail(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"))
        };

        [HttpGet("GetMyDocuments")]
        public async Task<IActionResult> GetMyDocuments()
        {
            if (Me is not int me) return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            try { return Ok(ApiResponseDto<List<MyPublicDocumentDto>>.CreateSuccess(await _service.GetMyDocuments(me), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        /// <summary>Opening a file records that the employee viewed this version.</summary>
        [HttpGet("ViewFile")]
        public async Task<IActionResult> ViewFile(int id)
        {
            if (Me is not int me) return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            try
            {
                var file = await _service.GetMyDocumentFile(id, me, Ip, Agent);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("Acknowledge")]
        public async Task<IActionResult> Acknowledge(int idGeneralDocumentRecord)
        {
            if (Me is not int me) return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            try
            {
                var when = await _service.AcknowledgeMyDocument(idGeneralDocumentRecord, me, Ip, Agent);
                return Ok(ApiResponseDto<DateTime>.CreateSuccess(when, "Thank you. Your acknowledgement has been recorded."));
            }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
