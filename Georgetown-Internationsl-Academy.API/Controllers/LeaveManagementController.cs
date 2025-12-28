using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    //[Authorize]
    public class LeaveManagementController : ControllerBase
    {

        private readonly ILeaveManaementServices _leaveService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public LeaveManagementController(
            ILeaveManaementServices leaveService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService)
        {
            _leaveService = leaveService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        #region Leave Types

        [HttpGet("GetLeaveTypes")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            try
            {
                var result = await _leaveService.GetLeaveTypes();

                if (!result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveTypesDto>(), "No leave types available."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                    .CreateSuccess(result, "Leave types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateLeaveTypes")]
        public async Task<IActionResult> AddOrUpdateLeaveTypes([FromBody] List<LeaveTypesDto> leaveTypeDtos)
        {
            if (leaveTypeDtos == null || !leaveTypeDtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            int loggedInEmployeeId = int.Parse(idEmployee);

            try
            {
                var screenCode = _configuration["ScreenCodes:LeaveTypes"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var success = await _leaveService.AddOrUpdateLeaveTypes(leaveTypeDtos);

                if (!success)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save leave types."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave types saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Annual Leave Config

        [HttpGet("GetAnnualLeaveTypeConfigs")]
        public async Task<IActionResult> GetAnnualLeaveTypeConfigs(int? idAnnualLeaveTypeConfig,int? idLeaveType,int? idYear,bool? isActive)
        {
            try
            {
                var result = await _leaveService.GetAnnualLeaveTypeConfigs(
                    idAnnualLeaveTypeConfig,
                    idLeaveType,
                    idYear,
                    isActive
                );

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<AnnualLeaveTypeConfigDto>>
                        .CreateSuccess(Enumerable.Empty<AnnualLeaveTypeConfigDto>(), "No annual leave configs found."));
                }

                return Ok(ApiResponseDto<IEnumerable<AnnualLeaveTypeConfigDto>>
                    .CreateSuccess(result, "Annual leave configuration retrieved."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddUpdateAnnualLeaveTypeConfig")]
        public async Task<IActionResult> AddUpdateAnnualLeaveTypeConfig([FromBody] List<AnnualLeaveTypeConfigDto> configDtos)
        {
            if (configDtos == null || !configDtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            int loggedInEmployeeId = int.Parse(userId);

            try
            {
                var screenCode = _configuration["ScreenCodes:AnnualLeaveConfig"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var ids = await _leaveService.AddUpdateAnnualLeaveTypeConfig(configDtos, loggedInEmployeeId);

                return Ok(ApiResponseDto<object>.CreateSuccess(new
                {
                    SuccessFlag = true,        
                    Message = "Annual leave configuration saved successfully.",
                    IdAnnualLeaveTypeConfig = ids
                }, "Annual leave configuration saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
[HttpPost("DeactivateAnnualLeaveTypeConfig")]
        public async Task<IActionResult> DeactivateAnnualLeaveTypeConfig(int idAnnualLeaveTypeConfig)
        {
            try
            {
                if (idAnnualLeaveTypeConfig <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdAnnualLeaveTypeConfig is required."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:AnnualLeaveConfig"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.DeactivateAnnualLeaveTypeConfig(idAnnualLeaveTypeConfig, loggedInEmployeeId);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to deactivate annual leave configuration."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Annual leave configuration deactivated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        #endregion

        #region Leave Template
        [HttpGet("GetLeaveTemplates")]
        public async Task<IActionResult> GetLeaveTemplates(int? idLeaveTemplate, bool? isActive, string? searchText)
        {
            try
            {
                var result = await _leaveService.GetLeaveTemplates(idLeaveTemplate, isActive, searchText);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveTemplateDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveTemplateDto>(), "No leave templates found."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveTemplateDto>>
                    .CreateSuccess(result, "Leave templates retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetLeaveTemplate")]
        public async Task<IActionResult> GetLeaveTemplate(int idLeaveTemplate)
        {
            try
            {
                if (idLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveTemplate is required."));

                var result = await _leaveService.GetLeaveTemplate(idLeaveTemplate);

                return Ok(ApiResponseDto<LeaveTemplateWithDetailsDto>
                    .CreateSuccess(result, "Leave template retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpPost("AddUpdateLeaveTemplate")]
        public async Task<IActionResult> AddUpdateLeaveTemplate([FromBody] LeaveTemplatePostDto dto)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.AddUpdateLeaveTemplate(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<LeaveTemplateSaveResponseDto>
                    .CreateSuccess(result, result.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpPost("DeactivateLeaveTemplate")]
        public async Task<IActionResult> DeactivateLeaveTemplate(int idLeaveTemplate)
        {
            try
            {
                if (idLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveTemplate is required."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var success = await _leaveService.DeactivateLeaveTemplate(idLeaveTemplate, loggedInEmployeeId);

                if (!success)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to deactivate leave template."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave template deactivated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        #endregion

    }
}
