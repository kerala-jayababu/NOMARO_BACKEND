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
    public class EmployeeOffBoardingController : ControllerBase
    {
        private readonly IEmployeeOffBoarding _EmpOffBoardingService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public EmployeeOffBoardingController(IEmployeeOffBoarding empOffBoardingService, IConfiguration configuration, IRoleBasedScreenService roleBasedService)
        {
            _EmpOffBoardingService = empOffBoardingService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        [HttpGet("GetExitReasons")]
        public async Task<IActionResult> GetExitReasons()
        => Ok(ApiResponseDto<IEnumerable<ExitReasonDto>>
            .CreateSuccess(await _EmpOffBoardingService.GetExitReasons(), "Exit reasons retrieved successfully."));
        [HttpPost("AddOrUpdateExitReasons")]
        public async Task<IActionResult> AddOrUpdateExitReasons(List<ExitReasonDto> exitReasonDtos)
        {
            if (exitReasonDtos == null || !exitReasonDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of exit reasons."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OffBoardingSetup"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _EmpOffBoardingService.AddOrUpdateExitReasons(exitReasonDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update exit reasons."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Exit reasons added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetExitTypes")]
        public async Task<IActionResult> GetExitTypes()
            => Ok(ApiResponseDto<IEnumerable<ExitTypeDto>>
                .CreateSuccess(await _EmpOffBoardingService.GetExitTypes(), "Exit types retrieved successfully."));

        [HttpPost("AddOrUpdateExitTypes")]
        public async Task<IActionResult> AddOrUpdateExitTypes(List<ExitTypeDto> exitTypeDtos)
        {
            if (exitTypeDtos == null || !exitTypeDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of exit types."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OffBoardingSetup"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _EmpOffBoardingService.AddOrUpdateExitTypes(exitTypeDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update exit types."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Exit types added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetNoticePeriodPolicies")]
        public async Task<IActionResult> GetNoticePeriodPolicies()
            => Ok(ApiResponseDto<IEnumerable<NoticePeriodPolicyDto>>
                .CreateSuccess(await _EmpOffBoardingService.GetNoticePeriodPolicies(), "Notice policies retrieved successfully."));

        [HttpPost("AddOrUpdateNoticePeriodPolicies")]
        public async Task<IActionResult> AddOrUpdateNoticePeriodPolicies(
            List<NoticePeriodPolicyDto> noticePolicyDtos)
        {
            if (noticePolicyDtos == null || !noticePolicyDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of notice period policies."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OffBoardingSetup"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _EmpOffBoardingService
                    .AddOrUpdateNoticePeriodPolicies(noticePolicyDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update notice period policies."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Notice period policies added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetClearanceTemplates")]
        public async Task<IActionResult> GetClearanceTemplates()
            => Ok(ApiResponseDto<IEnumerable<ClearanceTemplateDto>>
                .CreateSuccess(await _EmpOffBoardingService.GetClearanceTemplates(), "Clearance templates retrieved successfully."));

        [HttpPost("AddOrUpdateClearanceTemplates")]
        public async Task<IActionResult> AddOrUpdateClearanceTemplates(
            List<ClearanceTemplateDto> clearanceTemplateDtos)
        {
            if (clearanceTemplateDtos == null || !clearanceTemplateDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of clearance templates."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OffBoardingSetup"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _EmpOffBoardingService
                    .AddOrUpdateClearanceTemplates(clearanceTemplateDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update clearance templates."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Clearance templates added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

    }
}
