using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeSalaryConfigController : ControllerBase
    {
        private readonly IEmployeeSalaryConfigService _employeeSalaryConfigService;
        private readonly IValidator<EmployeeSalaryConfigDto> _configValidator;
        private readonly IValidator<EmployeeSalaryConfigDetailsDto> _detailsValidator;
        private readonly ISalaryHeadServices _salaryservice;
        public EmployeeSalaryConfigController(
            IEmployeeSalaryConfigService employeeSalaryConfigService,
            IValidator<EmployeeSalaryConfigDto> configValidator,
            IValidator<EmployeeSalaryConfigDetailsDto> detailsValidator,
            ISalaryHeadServices salaryservice)
        {
            _employeeSalaryConfigService = employeeSalaryConfigService;
            _configValidator = configValidator;
            _detailsValidator = detailsValidator;
            _salaryservice = salaryservice;
        }

        #region EmployeeSalaryConfig

        [HttpGet("GetAllEmployeeSalaryConfig")]
        public async Task<IActionResult> GetAllEmployeeSalaryConfig()
        {
            try
            {
                var configs = await _employeeSalaryConfigService.GetAllConfigs();
                if (!configs.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No configurations found."));

                return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDto>>.CreateSuccess(configs, "EmployeeSalaryConfig retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeSalaryConfigById/{id}")]
        public async Task<IActionResult> GetEmployeeSalaryConfigById(int id)
        {
            try
            {
                var config = await _employeeSalaryConfigService.GetConfigById(id);
                if (config == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Configuration not found."));

                return Ok(ApiResponseDto<EmployeeSalaryConfigDto>.CreateSuccess(config, "EmployeeSalaryConfigById retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddEmployeeSalaryConfig")]
        public async Task<IActionResult> AddEmployeeSalaryConfig([FromBody] EmployeeSalaryConfigDto dto)
        {
            var validationResult = await _configValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _employeeSalaryConfigService.AddConfig(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add configuration."));

                return Ok(ApiResponseDto<EmployeeSalaryConfigDto>.CreateSuccess(result, "EmployeeSalaryConfig added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateEmployeeSalaryConfig")]
        public async Task<IActionResult> UpdateEmployeeSalaryConfig([FromBody] EmployeeSalaryConfigDto dto)
        {
            var validationResult = await _configValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _employeeSalaryConfigService.UpdateConfig(dto);
                if (result == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Configuration not found."));

                return Ok(ApiResponseDto<EmployeeSalaryConfigDto>.CreateSuccess(result, "EmployeeSalaryConfig updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region EmployeeSalaryConfigDetails

        [HttpGet("GetAllEmployeeSalaryConfigDetails")]
        public async Task<IActionResult> GetAllEmployeeSalaryConfigDetails()
        {
            try
            {
                var details = await _employeeSalaryConfigService.GetAllDetails();
                if (!details.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No details found."));

                return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDetailsDto>>.CreateSuccess(details, "GetAllEmployeeSalaryConfigDetails retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeSalaryConfigDetailsById/{id}")]
        public async Task<IActionResult> GetEmployeeSalaryConfigDetailsById(int id)
        {
            try
            {
                var detail = await _employeeSalaryConfigService.GetDetailById(id);
                if (detail == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Detail not found."));

                return Ok(ApiResponseDto<EmployeeSalaryConfigDetailsDto>.CreateSuccess(detail, "EmployeeSalaryConfigDetailsById retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddEmployeeSalaryConfigDetails")]
        public async Task<IActionResult> AddEmployeeSalaryConfigDetails([FromBody] EmployeeSalaryConfigDetailsDto dto)
        {
            var validationResult = await _detailsValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }
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

            try
            {
                var result = await _employeeSalaryConfigService.AddDetail(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("EmployeeSalaryConfigDetails failed to add detail."));
                return Ok(ApiResponseDto<string>.CreateSuccess( "EmployeeSalaryConfigDetails added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateEmployeeSalaryConfigDetails")]
        public async Task<IActionResult> UpdateEmployeeSalaryConfigDetails([FromBody] EmployeeSalaryConfigDetailsDto dto)
        {
            var validationResult = await _detailsValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }
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

            try
            {
                var result = await _employeeSalaryConfigService.UpdateDetail(dto);
                if (result == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("EmployeeSalaryConfigDetails not found."));
                return Ok(ApiResponseDto<string>.CreateSuccess("EmployeeSalaryConfigDetails updated successfully."));
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
