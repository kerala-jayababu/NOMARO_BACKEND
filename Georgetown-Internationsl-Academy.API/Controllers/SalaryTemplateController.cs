using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Georgetown_Internationsl_Academy.API.Controllers
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
        public async Task<IActionResult> GetAll(string? searchText = null, string? dropdownFilter = null)
        {
            try
            {
                var templates = await _salaryTemplateService.GetAllSalaryTemplates(searchText,dropdownFilter);
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
            // Step 2: Check custom formulas in SalaryTemplateDetails
            if (dto.SalaryTemplateDetails != null && dto.SalaryTemplateDetails.Any())
            {
                foreach (var detail in dto.SalaryTemplateDetails)
                {
                    if (detail.CalculationMethod == "FORMULA" && !string.IsNullOrWhiteSpace(detail.CustomFormula))
                    {
                        // Extract components from formula
                        var formulaComponents = ExtractComponentsFromFormula(detail.CustomFormula);
                        if (!formulaComponents.Any())
                        {
                            return BadRequest(ApiResponseDto<string>.CreateFailure(
                                "Custom formula is invalid or contains no valid components."
                            ));
                        }
                        // Fetch all valid SalaryHead codes
                        var salaryHeads = await _salaryservice.GetSalaryHeadList();
                        var validSalaryHeadCodes = salaryHeads.Select(sh => sh.SalaryHeadCode).ToList();

                        // Find invalid components in the formula
                        var invalidComponents = formulaComponents
                            .Where(component => !validSalaryHeadCodes.Contains(component))
                            .ToList();

                        if (invalidComponents.Any())
                        {
                            return BadRequest(ApiResponseDto<string>.CreateFailure(
                                $"The following components in custom formula are invalid: {string.Join(", ", invalidComponents)}"
                            ));
                        }
                    }
                }
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
            // Step 2: Check custom formulas in SalaryTemplateDetails
            if (dto.SalaryTemplateDetails != null && dto.SalaryTemplateDetails.Any())
            {
                foreach (var detail in dto.SalaryTemplateDetails)
                {
                    if (detail.CalculationMethod == "FORMULA" && !string.IsNullOrWhiteSpace(detail.CustomFormula))
                    {
                        // Extract components from formula
                        var formulaComponents = ExtractComponentsFromFormula(detail.CustomFormula);
                        if (!formulaComponents.Any())
                        {
                            return BadRequest(ApiResponseDto<string>.CreateFailure(
                                "Custom formula is invalid or contains no valid components."
                            ));
                        }
                        // Fetch all valid SalaryHead codes
                        var salaryHeads = await _salaryservice.GetSalaryHeadList();
                        var validSalaryHeadCodes = salaryHeads.Select(sh => sh.SalaryHeadCode).ToList();

                        // Find invalid components in the formula
                        var invalidComponents = formulaComponents
                            .Where(component => !validSalaryHeadCodes.Contains(component))
                            .ToList();

                        if (invalidComponents.Any())
                        {
                            return BadRequest(ApiResponseDto<string>.CreateFailure(
                                $"The following components in custom formula are invalid: {string.Join(", ", invalidComponents)}"
                            ));
                        }
                    }
                }
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
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        private List<string> ExtractComponentsFromFormula(string formula)
        {
            // Use a regular expression to extract alphanumeric components from the formula
            var regex = new Regex(@"\b[A-Za-z]+\b");
            var matches = regex.Matches(formula);

            return matches.Select(match => match.Value).Distinct().ToList();
        }

        #endregion


    }
}
