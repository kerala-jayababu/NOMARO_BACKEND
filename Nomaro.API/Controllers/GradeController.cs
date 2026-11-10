using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class GradeController : ControllerBase
    {
        private readonly IGradeService _gradeService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IValidator<GradeDto> _gradeValidator;

        public GradeController(
            IGradeService gradeService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService,
            IValidator<GradeDto> gradeValidator)
        {
            _gradeService = gradeService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _gradeValidator = gradeValidator;
        }

        // List of grades. isActive = true gives only active grades (used by the Employee combo).
        [HttpGet("GetGrades")]
        public async Task<IActionResult> GetGrades(bool? isActive)
        {
            try
            {
                var grades = await _gradeService.GetGrades(isActive);

                if (grades == null || !grades.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<GradeDto>>
                        .CreateSuccess(Enumerable.Empty<GradeDto>(), "No grades available."));
                }

                return Ok(ApiResponseDto<IEnumerable<GradeDto>>
                    .CreateSuccess(grades, "Grades retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateGrade")]
        public async Task<IActionResult> AddOrUpdateGrade([FromBody] GradeDto gradeDto)
        {
            var validationResult = await _gradeValidator.ValidateAsync(gradeDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var isUpdate = gradeDto.IdGrade > 0;
                var screenCode = _configuration["ScreenCodes:Grades"] ?? "GRADES";
                var actionType = isUpdate ? "U" : "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);
                if (!hasPermission)
                {
                    return StatusCode(403, ApiResponseDto<string>
                        .CreateFailure("You do not have permission to perform this action."));
                }

                if (isUpdate && await _gradeService.GetGradeById(gradeDto.IdGrade) == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Grade not found."));
                }

                if (await _gradeService.IsGradeCodeExists(gradeDto.GradeCode, gradeDto.IdGrade))
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Grade code already exists."));
                }

                await _gradeService.AddOrUpdateGrade(gradeDto, int.Parse(IdEmployee));

                return Ok(ApiResponseDto<string>.CreateSuccess(
                    isUpdate ? "Grade updated successfully." : "Grade added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
