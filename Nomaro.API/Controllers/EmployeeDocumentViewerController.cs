using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// View / download documents uploaded against employee profiles. Needs "V" on screen EMPDOCVIEW.
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeDocumentViewerController : ControllerBase
    {
        private readonly IEmployeeDocumentViewerService _service;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IConfiguration _configuration;

        public EmployeeDocumentViewerController(IEmployeeDocumentViewerService service, IRoleBasedScreenService roleBasedService, IConfiguration configuration)
        {
            _service = service;
            _roleBasedService = roleBasedService;
            _configuration = configuration;
        }

        private async Task<IActionResult?> CheckView()
        {
            if (!int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var me))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            var ok = await _roleBasedService.CheckEmployeePermission(me, (_configuration["ScreenCodes:EmployeeDocuments"] ?? "EMPDOCVIEW"), "V");
            return ok ? null : StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
        }

        [HttpGet("GetDocuments")]
        public async Task<IActionResult> GetDocuments([FromQuery] EmployeeDocumentFilterDto filter)
        {
            var denied = await CheckView();
            if (denied != null) return denied;
            try
            {
                var list = await _service.GetDocuments(filter ?? new EmployeeDocumentFilterDto());
                return Ok(ApiResponseDto<List<EmployeeDocumentListDto>>.CreateSuccess(list, "Documents retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("DownloadDocument")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var denied = await CheckView();
            if (denied != null) return denied;
            try
            {
                var file = await _service.GetDocumentFile(id);
                return file == null
                    ? NotFound(ApiResponseDto<string>.CreateFailure("The file for this document was not found on the server."))
                    : File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
