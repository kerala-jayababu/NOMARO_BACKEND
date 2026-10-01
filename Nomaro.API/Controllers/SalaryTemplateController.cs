using Asp.Versioning;
using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Nomaro.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class SalaryTemplateController : ControllerBase
    {
        private readonly ISalaryTemplateService _salaryTemplateService;
        private readonly IValidator<SalaryTemplateDto> _salaryTemplateValidator;
        private readonly IValidator<SalaryTemplateDto> _salaryTemplateUpdateValidator;
        private readonly ISalaryTemplateDetailsService _salaryTemplateDetailsService;
        private readonly ISalaryHeadServices _salaryservice;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public SalaryTemplateController(ISalaryTemplateService salaryTemplateService, IValidator<SalaryTemplateDto> salaryTemplateValidator,
            IConfiguration configuration, IRoleBasedScreenService roleBasedService,
            IValidator<SalaryTemplateDto> salaryTemplateUpdateValidator, ISalaryTemplateDetailsService salaryTemplateDetailsService, ISalaryHeadServices salaryservice)
        {
            _salaryTemplateService = salaryTemplateService;
            _salaryTemplateValidator = salaryTemplateValidator;
            _salaryTemplateUpdateValidator = salaryTemplateUpdateValidator;
            _salaryTemplateDetailsService = salaryTemplateDetailsService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _salaryservice = salaryservice;

        }

        #region SalaryTemplate

        [HttpGet("GetAllSalaryTemplates")]
        public async Task<IActionResult> GetAll(string? searchText = null, string? dropdownFilter = null, bool includeInactive = false)
        {
            try
            {
                var templates = await _salaryTemplateService.GetAllSalaryTemplates(searchText, dropdownFilter, includeInactive);
                if (templates == null || !templates.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryTemplateDto>>.CreateSuccess(Enumerable.Empty<SalaryTemplateDto>(), "No salary templates found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryTemplateDto>>.CreateSuccess(templates, "Salary templates retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetSalaryTemplateById")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var template = await _salaryTemplateService.GetSalaryTemplateById(id);
                if (template == null)
                {
                    return Ok(ApiResponseDto<SalaryTemplateDto>.CreateSuccess(null, "Salary template not found."));
                }

                return Ok(ApiResponseDto<SalaryTemplateDto>.CreateSuccess(template, "Salary template retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddSalaryTemplate")]
        public async Task<IActionResult> AddSalaryTemplate([FromBody] SalaryTemplateDto dto)
        {
            var validationResult = await _salaryTemplateValidator.ValidateAsync(dto);
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
            var screenCode = _configuration["ScreenCodes:SalaryTemplates"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _salaryTemplateService.AddSalaryTemplate(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add salary template."));
                }

                return Ok(ApiResponseDto<object>.CreateSuccess(true, "Salary template added successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateSalaryTemplate")]
        public async Task<IActionResult> UpdateSalaryTemplate([FromBody] SalaryTemplateDto dto)
        {
            var validationResult = await _salaryTemplateUpdateValidator.ValidateAsync(dto);
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
            var screenCode = _configuration["ScreenCodes:SalaryTemplates"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _salaryTemplateService.UpdateSalaryTemplate(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary template not found for update."));
                }

                return Ok(ApiResponseDto<object>.CreateSuccess(true, "Salary template updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>
        /// Calculates the grid (Calculated Value per row and the totals line) the same way the salary procedure does.
        /// Call it after every change on the Salary Template screen. Nothing is saved.
        /// </summary>
        [HttpPost("CalculateSalaryStructure")]
        public async Task<IActionResult> CalculateSalaryStructure([FromBody] SalaryStructureCalculationRequestDto request)
        {
            if (request == null || request.Rows == null)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));
            }

            try
            {
                var result = await _salaryTemplateService.CalculateSalaryStructure(request.Rows);
                return Ok(ApiResponseDto<SalaryStructureResultDto>.CreateSuccess(result,
                    result.Errors.Any() ? string.Join(" ", result.Errors) : "Salary structure calculated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion


    }
}

