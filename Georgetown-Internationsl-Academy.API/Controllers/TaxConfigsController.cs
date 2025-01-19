using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(1)]
[Route("/api/v{v:apiVersion}/[controller]")]
public class TaxConfigsController : ControllerBase
{
    private readonly ITaxConfigService _taxConfigService;
    private readonly IValidator<TaxConfigManageDto> _validator;

    public TaxConfigsController(ITaxConfigService taxConfigService, IValidator<TaxConfigManageDto> validator)
    {
        _taxConfigService = taxConfigService;
        _validator = validator;
    }

    [HttpGet("GetAllTaxConfigs")]
    public async Task<IActionResult> GetAllTaxConfigs()
    {
        try
        {
            var taxConfigs = await _taxConfigService.GetAllTaxConfigs();

            if (!taxConfigs.Any())
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("No tax configs found."));
            }

            return Ok(ApiResponseDto<IEnumerable<TaxConfigDto>>.CreateSuccess(taxConfigs, "Tax configs retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("GetTaxConfigById")]
    public async Task<IActionResult> GetTaxConfigById(int id)
    {
        try
        {
            var taxConfig = await _taxConfigService.GetTaxConfigById(id);

            if (taxConfig == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("Tax config not found."));
            }

            return Ok(ApiResponseDto<TaxConfigDto>.CreateSuccess(taxConfig, "Tax config retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("AddTaxConfig")]
    public async Task<IActionResult> AddTaxConfig([FromBody] TaxConfigManageDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        try
        {
            var result = await _taxConfigService.AddTaxConfig(dto);

            if (result == null)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add tax config."));
            }

            return Ok(ApiResponseDto<string>.CreateSuccess("Tax config added successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("UpdateTaxConfig")]
    public async Task<IActionResult> UpdateTaxConfig([FromBody] TaxConfigManageDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        try
        {
            var result = await _taxConfigService.UpdateTaxConfig(dto);

            if (result == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("Tax config not found."));
            }

            return Ok(ApiResponseDto<string>.CreateSuccess("Tax config updated successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }
}
