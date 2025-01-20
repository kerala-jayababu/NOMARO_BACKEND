using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class SalaryTemplateController : ControllerBase
    {
        private readonly ISalaryTemplateService _salaryTemplateService;
        private readonly IValidator<SalaryTemplateManageDto> _salaryTemplateValidator;
        private readonly IValidator<SalaryTemplateDto> _salaryTemplateUpdateValidator;

        public SalaryTemplateController(ISalaryTemplateService salaryTemplateService, IValidator<SalaryTemplateManageDto> salaryTemplateValidator, IValidator<SalaryTemplateDto> salaryTemplateUpdateValidator)
        {
            _salaryTemplateService = salaryTemplateService;
            _salaryTemplateValidator = salaryTemplateValidator;
            _salaryTemplateUpdateValidator = salaryTemplateUpdateValidator;
        }

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

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary template added successfully."));
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

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary template updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
