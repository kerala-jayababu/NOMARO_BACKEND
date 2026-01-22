using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
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
        public async Task<IActionResult> AddOrUpdateExitReasons(ExitReasonDto exitReasonDto)
        {


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

                var isSuccess = await _EmpOffBoardingService.AddOrUpdateExitReasons(exitReasonDto);

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
        public async Task<IActionResult> AddOrUpdateExitTypes(ExitTypeDto exitTypeDto)
        {
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

                var isSuccess = await _EmpOffBoardingService.AddOrUpdateExitTypes(exitTypeDto);

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
        public async Task<IActionResult> AddOrUpdateNoticePeriodPolicies(NoticePeriodPolicyDto noticePolicyDto)
        {

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
                    .AddOrUpdateNoticePeriodPolicies(noticePolicyDto);

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
        public async Task<IActionResult> AddOrUpdateClearanceTemplates(ClearanceTemplateDto clearanceTemplateDto)
        {

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
                    .AddOrUpdateClearanceTemplates(clearanceTemplateDto);

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

        [HttpGet("GetClearanceTemplateDepartments/{idClearanceTemplate}")]
        public async Task<IActionResult> GetClearanceTemplateDepartments(int idClearanceTemplate)
        {
            try
            {
                var data = await _EmpOffBoardingService.GetClearanceTemplateDepartments(idClearanceTemplate);

                return Ok(ApiResponseDto<IEnumerable<ClearanceTemplateDepartmentDto>>
                    .CreateSuccess(data, "Clearance checklist retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateClearanceTemplateDepartments")]
        public async Task<IActionResult> AddOrUpdateClearanceTemplateDepartments(List<ClearanceTemplateDepartmentDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid checklist."));
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

                var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _EmpOffBoardingService.AddOrUpdateClearanceTemplateDepartment(dtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to save clearance checklist."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Clearance checklist saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpPost("SubmitResignation")]
        public async Task<IActionResult> SubmitResignation([FromBody] SubmitResignationDto resignationDto)
        {
            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idEmployee) || !int.TryParse(idEmployee, out int loggedInEmployeeId))
            {
                return Unauthorized(ApiResponseDto<SubmitResignationResponseDto>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                // Validate input
                if (resignationDto == null || resignationDto.IdEmployee <= 0)
                {
                    return BadRequest(ApiResponseDto<SubmitResignationResponseDto>
                        .CreateFailure("Invalid employee ID."));
                }

                if (resignationDto.IdExitReason <= 0)
                {
                    return BadRequest(ApiResponseDto<SubmitResignationResponseDto>
                        .CreateFailure("Invalid exit reason."));
                }

                if (resignationDto.ProposedLWD == default)
                {
                    return BadRequest(ApiResponseDto<SubmitResignationResponseDto>
                        .CreateFailure("Proposed last working day is required."));
                }

                // Call service to submit resignation
                var result = await _EmpOffBoardingService.SubmitResignation(resignationDto, loggedInEmployeeId);

                if (result.Success)
                {
                    return Ok(ApiResponseDto<SubmitResignationResponseDto>
                        .CreateSuccess(result, result.Message ?? "Resignation submitted successfully."));
                }
                else
                {
                    return BadRequest(ApiResponseDto<SubmitResignationResponseDto>
                        .CreateFailure(result.Message ?? "Failed to submit resignation."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<SubmitResignationResponseDto>
                        .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetResignationRequests")]
        public async Task<IActionResult> GetResignationRequests([FromQuery] string? roleType = null, [FromQuery] int? idEmployee = null, [FromQuery] DateTime? initiationDate = null)
        {
            var idLoggedInEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idLoggedInEmployee))
            {
                return Unauthorized(ApiResponseDto<IEnumerable<ResignationRequestDto>>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                // Call service to get resignation requests
                var result = await _EmpOffBoardingService.GetResignationRequests(
                    int.Parse(idLoggedInEmployee),
                    roleType,
                    idEmployee,
                    initiationDate);

                if (result != null && result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ResignationRequestDto>>
                        .CreateSuccess(result, "Resignation requests retrieved successfully."));
                }
                else
                {
                    return Ok(ApiResponseDto<IEnumerable<ResignationRequestDto>>
                        .CreateSuccess(new List<ResignationRequestDto>(), "No resignation requests found."));
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<IEnumerable<ResignationRequestDto>>
                    .CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<IEnumerable<ResignationRequestDto>>
                        .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("SubmitReportingOfficerActions")]
        public async Task<IActionResult> SubmitReportingOfficerActions(SubmitReportingOfficerActionsDto dto)
        {
            var idLoggedInEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idLoggedInEmployee))
            {
                return Unauthorized(ApiResponseDto<ReportingOfficerActionResponseDto>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                // Call service to process reporting officer action
                var result = await _EmpOffBoardingService.SubmitReportingOfficerActions(dto, int.Parse(idLoggedInEmployee));

                if (result.Success)
                {
                    return Ok(ApiResponseDto<ReportingOfficerActionResponseDto>
                        .CreateSuccess(result, result.Message ?? "Operation successful."));
                }
                else
                {
                    return BadRequest(ApiResponseDto<ReportingOfficerActionResponseDto>
                        .CreateFailure(result.Message ?? "Operation failed."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<ReportingOfficerActionResponseDto>
                        .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        #region RESIGNATION/EXIT CASES



        [HttpPost("DeleteClearanceTemplateDepartment")]
        public async Task<IActionResult> DeleteClearanceTemplateDepartment(int IdTemplateDept)
        {

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

                var isSuccess = await _EmpOffBoardingService.DeleteClearanceTemplateDepartment(IdTemplateDept);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to delete Clearance Template Detail"));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Clearance template Detail added/updated successfully."));
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