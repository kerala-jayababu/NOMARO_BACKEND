using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IValidator<SalaryTemplateManageDto> _salaryTemplateValidator;
        private readonly IValidator<SalaryTemplateDto> _salaryTemplateUpdateValidator;
        private readonly ISalaryTemplateDetailsService _salaryTemplateDetailsService;
        private readonly ISalaryHeadServices _salaryservice;

        public SalaryTemplateController(ISalaryTemplateService salaryTemplateService, IValidator<SalaryTemplateManageDto> salaryTemplateValidator, 
            IValidator<SalaryTemplateDto> salaryTemplateUpdateValidator, ISalaryTemplateDetailsService salaryTemplateDetailsService, ISalaryHeadServices salaryservice)
        {
            _salaryTemplateService = salaryTemplateService;
            _salaryTemplateValidator = salaryTemplateValidator;
            _salaryTemplateUpdateValidator = salaryTemplateUpdateValidator;
            _salaryTemplateDetailsService = salaryTemplateDetailsService;
            _salaryservice = salaryservice;

        }

        #region SalaryTemplate

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var templates = await _salaryTemplateService.GetAllSalaryTemplates();
                if (!templates.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salary templates found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryTemplateDto>>.CreateSuccess(templates, "Salary templates retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var template = await _salaryTemplateService.GetSalaryTemplateById(id);
                if (template == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary template not found."));
                }

                return Ok(ApiResponseDto<SalaryTemplateDto>.CreateSuccess(template, "Salary template retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] SalaryTemplateManageDto dto)
        {
            var validationResult = await _salaryTemplateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _salaryTemplateService.AddSalaryTemplate(dto);
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add salary template."));
                }

                return Ok(ApiResponseDto<object>.CreateSuccess(result,"Salary template added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody] SalaryTemplateDto dto)
        {
            var validationResult = await _salaryTemplateUpdateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _salaryTemplateService.UpdateSalaryTemplate(dto);
                if (result == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary template not found for update."));
                }

                return Ok(ApiResponseDto<SalaryTemplateDto>.CreateSuccess(result,"Salary template updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region SalaryTemplateDetails

        [HttpGet("GetAllSalaryTemplateDetails")]
        public async Task<IActionResult> GetAllSalaryTemplateDetails()
        {
            try
            {
                var details = await _salaryTemplateDetailsService.GetAllSalaryTemplateDetails();
                if (!details.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salary template details found."));

                return Ok(ApiResponseDto<IEnumerable<SalaryTemplateDetailDto>>.CreateSuccess(details, "Salary template details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetSalaryTemplateDetailById")]
        public async Task<IActionResult> GetSalaryTemplateDetailById(int id)
        {
            try
            {
                var detail = await _salaryTemplateDetailsService.GetSalaryTemplateDetailById(id);
                if (detail == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary template detail not found."));

                return Ok(ApiResponseDto<SalaryTemplateDetailDto>.CreateSuccess(detail, "Salary template detail retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddSalaryTemplateDetail")]
        public async Task<IActionResult> AddSalaryTemplateDetail([FromBody] SalaryTemplateDetailDto dto)
        {           

            try
            {
                if (dto.CalculationMethod == "FORMULA")
                {
                    // Extract the components from the formula
                    var formulaComponents = ExtractComponentsFromFormula(dto.CustomFormula);

                    // Query the database to check for missing components
                    var salaryHeads = await _salaryservice.GetSalaryHeadList();
                    var missingComponents = formulaComponents
                        .Where(component => !salaryHeads.Any(sh => sh.SalaryHeadCode == component))
                        .ToList();

                    // If there are missing components, return validation error
                    if (missingComponents.Any())
                    {
                        return BadRequest(ApiResponseDto<string>.CreateFailure($"The following components are not valid salary heads: {string.Join(", ", missingComponents)}"));
                    }
                }

                var result = await _salaryTemplateDetailsService.AddSalaryTemplateDetail(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create salary template detail."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary template detail created successfully."));
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

        [HttpPost("UpdateSalaryTemplateDetail")]
        public async Task<IActionResult> UpdateSalaryTemplateDetail([FromBody] SalaryTemplateDetailDto dto)
        {
            try
            {
                // Check if CalculationMethod is "Custom Formula"
                if (dto.CalculationMethod == "FORMULA")
                {
                    // Extract the components from the formula
                    var formulaComponents = ExtractComponentsFromFormula(dto.CustomFormula);

                    // Query the database to check for missing components
                    var salaryHeads = await _salaryservice.GetSalaryHeadList();
                    var missingComponents = formulaComponents
                        .Where(component => !salaryHeads.Any(sh => sh.SalaryHeadCode == component))
                        .ToList();

                    // If there are missing components, return validation error
                    if (missingComponents.Any())
                    {
                        return BadRequest(ApiResponseDto<string>.CreateFailure($"The following components are not valid salary heads: {string.Join(", ", missingComponents)}"));
                    }
                }

                // Call the service to update the salary template detail
                var result = await _salaryTemplateDetailsService.UpdateSalaryTemplateDetail(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update salary template detail."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary template detail updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

    }
}
