using Asp.Versioning;
using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[ApiVersion(1)]
[Route("/api/v{v:apiVersion}/[controller]")]
[Authorize]
public class TaxConfigsController : ControllerBase
{
    private readonly ITaxSlabService _taxSlabService;
    private readonly IValidator<TaxSlabDto> _taxSlabValidator;
    private readonly ITaxYearConfigService _taxYearConfigService;
    private readonly IValidator<TaxYearConfigDto> _taxYearConfigValidator;
    private readonly IConfiguration _configuration;
    private readonly IRoleBasedScreenService _roleBasedService;

    public TaxConfigsController(ITaxSlabService taxSlabService, IValidator<TaxSlabDto> taxSlabValidator,
        ITaxYearConfigService taxYearConfigService, IValidator<TaxYearConfigDto> taxYearConfigValidator,
        IConfiguration configuration, IRoleBasedScreenService roleBasedService)
    {
        _taxSlabService = taxSlabService;
        _taxSlabValidator = taxSlabValidator;
        _taxYearConfigService = taxYearConfigService;
        _taxYearConfigValidator = taxYearConfigValidator;
        _configuration = configuration;
        _roleBasedService = roleBasedService;
    }

    /// <summary>Checks the IncomeTaxConfig screen permission; returns an error result, or null when allowed.</summary>
    private async Task<IActionResult?> CheckPermission(string actionType)
    {
        var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(IdEmployee))
        {
            return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
        }

        var screenCode = _configuration["ScreenCodes:IncomeTaxConfig"];
        var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);
        if (!hasPermission)
        {
            return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
        }
        return null;
    }

    #region TaxYearConfigs

    /// <summary>Tax year configurations (financial year + regime); all, or those of one financial year.</summary>
    [HttpGet("GetTaxYearConfigs")]
    public async Task<IActionResult> GetTaxYearConfigs(int? idFinancialYear = null)
    {
        try
        {
            var configs = await _taxYearConfigService.GetTaxYearConfigs(idFinancialYear);
            if (configs == null || !configs.Any())
            {
                return Ok(ApiResponseDto<IEnumerable<TaxYearConfigDto>>.CreateSuccess(Enumerable.Empty<TaxYearConfigDto>(), "No tax year configurations found."));
            }

            return Ok(ApiResponseDto<IEnumerable<TaxYearConfigDto>>.CreateSuccess(configs, "Tax year configurations retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("GetTaxYearConfigById")]
    public async Task<IActionResult> GetTaxYearConfigById(int id)
    {
        try
        {
            var config = await _taxYearConfigService.GetTaxYearConfigById(id);
            if (config == null)
            {
                return Ok(ApiResponseDto<TaxYearConfigDto>.CreateSuccess(null, "Tax year configuration not found."));
            }

            return Ok(ApiResponseDto<TaxYearConfigDto>.CreateSuccess(config, "Tax year configuration retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("AddOrUpdateTaxYearConfig")]
    public async Task<IActionResult> AddOrUpdateTaxYearConfig([FromBody] TaxYearConfigDto dto)
    {
        var validationResult = await _taxYearConfigValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        var denied = await CheckPermission(dto.IdTaxYearConfig > 0 ? "U" : "A");
        if (denied != null) return denied;

        try
        {
            var isSuccess = await _taxYearConfigService.AddOrUpdateTaxYearConfig(dto);
            if (!isSuccess)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save the tax year configuration."));
            }

            var message = dto.IdTaxYearConfig > 0 ? "Tax year configuration updated successfully." : "Tax year configuration added successfully.";
            return Ok(ApiResponseDto<string>.CreateSuccess(message, message));
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

    #endregion

    #region TaxSlabs

    /// <summary>Slabs of a tax year configuration, optionally for one age category (ALL, BELOW60, SENIOR, SUPER).</summary>
    [HttpGet("GetAllTaxSlabs")]
    public async Task<IActionResult> GetAllTaxSlabs(int idTaxYearConfig, string? ageCategory = null)
    {
        if (idTaxYearConfig <= 0)
        {
            return BadRequest(ApiResponseDto<string>.CreateFailure("Select the financial year and tax regime."));
        }

        try
        {
            var taxSlabs = await _taxSlabService.GetAllTaxSlabs(idTaxYearConfig, ageCategory);
            if (taxSlabs == null || !taxSlabs.Any())
            {
                return Ok(ApiResponseDto<IEnumerable<TaxSlabDto>>.CreateSuccess(Enumerable.Empty<TaxSlabDto>(), "No tax slabs found."));
            }

            return Ok(ApiResponseDto<IEnumerable<TaxSlabDto>>.CreateSuccess(taxSlabs, "Tax slabs retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("GetTaxSlabById")]
    public async Task<IActionResult> GetTaxSlabById(int id)
    {
        try
        {
            var taxSlab = await _taxSlabService.GetTaxSlabById(id);
            if (taxSlab == null)
            {
                return Ok(ApiResponseDto<TaxSlabDto>.CreateSuccess(null, "Tax slab not found."));
            }

            return Ok(ApiResponseDto<TaxSlabDto>.CreateSuccess(taxSlab, "Tax slab retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("AddTaxSlab")]
    public async Task<IActionResult> AddTaxSlab([FromBody] TaxSlabDto dto)
    {
        var validationResult = await _taxSlabValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        var denied = await CheckPermission("A");
        if (denied != null) return denied;

        try
        {
            var result = await _taxSlabService.AddTaxSlab(dto);
            if (result == null)
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add tax slab."));

            return Ok(ApiResponseDto<string>.CreateSuccess("Tax slab added successfully.", "Tax slab added successfully."));
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

    [HttpPost("UpdateTaxSlab")]
    public async Task<IActionResult> UpdateTaxSlab([FromBody] TaxSlabDto dto)
    {
        var validationResult = await _taxSlabValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        var denied = await CheckPermission("U");
        if (denied != null) return denied;

        try
        {
            var result = await _taxSlabService.UpdateTaxSlab(dto);
            if (result == null)
                return NotFound(ApiResponseDto<string>.CreateFailure($"No tax slab found with ID: {dto.IdTaxSlab}"));

            return Ok(ApiResponseDto<string>.CreateSuccess("Tax slab updated successfully.", "Tax slab updated successfully."));
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

    #endregion
}
