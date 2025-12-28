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
        public async Task<IActionResult> AddOrUpdateLeaveTypes(
            List<LeaveTypesDto> leaveTypeDtos)
        {
            if (leaveTypeDtos == null || !leaveTypeDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input."));
            }

            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:LeaveTypes"];
                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(idEmployee), screenCode, "A");

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("Permission denied."));
                }

                var success = await _leaveService.AddOrUpdateLeaveTypes(leaveTypeDtos);
                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Leave types saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Annual Leave Config

        [HttpGet("GetAnnualLeaveTypeConfigs")]
        public async Task<IActionResult> GetAnnualLeaveTypeConfigs(int idYear)
        {
            try
            {
                var result = await _leaveService.GetAnnualLeaveTypeConfigs(idYear);
                return Ok(ApiResponseDto<IEnumerable<AnnualLeaveTypeConfigDto>>
                    .CreateSuccess(result, "Annual leave configuration retrieved."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateAnnualLeaveTypeConfigs")]
        public async Task<IActionResult> AddOrUpdateAnnualLeaveTypeConfigs(
            List<AnnualLeaveTypeConfigDto> configDtos)
        {
            if (configDtos == null || !configDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input."));
            }

            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:AnnualLeaveConfig"];
                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(idEmployee), screenCode, "A");

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("Permission denied."));
                }

                var success = await _leaveService
                    .AddOrUpdateAnnualLeaveTypeConfigs(configDtos);

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Annual leave configuration saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion
    }
}
