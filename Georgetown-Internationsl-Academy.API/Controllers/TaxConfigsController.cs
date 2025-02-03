using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(1)]
[Route("/api/v{v:apiVersion}/[controller]")]
[Authorize]
public class TaxConfigsController : ControllerBase
{
    private readonly ITaxConfigService _taxConfigService;
    private readonly IValidator<TaxConfigManageDto> _validator;
    private readonly ITaxSlabService _taxSlabService;
    private readonly IValidator<TaxSlabDto> _taxSlabValidator;
    private readonly IChildTaxThresholdService _childTaxservice;
    private readonly IValidator<ChildTaxThresholdDto> _childTaxvalidator;
    public TaxConfigsController(ITaxConfigService taxConfigService, IValidator<TaxConfigManageDto> validator, ITaxSlabService taxSlabService,
        IValidator<TaxSlabDto> taxSlabValidator, IChildTaxThresholdService childTaxservice, IValidator<ChildTaxThresholdDto> childTaxvalidator)
    {
        _taxConfigService = taxConfigService;
        _validator = validator;
        _taxSlabService = taxSlabService;
        _taxSlabValidator = taxSlabValidator;
        _childTaxservice = childTaxservice;
        _childTaxvalidator = childTaxvalidator;
    }


    #region TaxConfig

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

    #endregion

    #region TaxSlabs
    [HttpGet("GetAllTaxSlabs")]
    public async Task<IActionResult> GetAllTaxSlabs()
    {
        try
        {
            var taxSlabs = await _taxSlabService.GetAllTaxSlabs();
            if (!taxSlabs.Any())
                return NotFound("No tax slabs found.");

            return Ok(taxSlabs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet("GetTaxSlabById")]
    public async Task<IActionResult> GetTaxSlabById(int id)
    {
        try
        {
            var taxSlab = await _taxSlabService.GetTaxSlabById(id);
            if (taxSlab == null)
                return NotFound($"No tax slab found with ID: {id}");

            return Ok(taxSlab);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("AddTaxSlab")]
    public async Task<IActionResult> AddTaxSlab([FromBody] TaxSlabDto dto)
    {
        var validationResult = await _taxSlabValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest($"Validation failed: {errors}");
        }

        try
        {
            var result = await _taxSlabService.AddTaxSlab(dto);
            if (result == null)
                return StatusCode(500, "Failed to add tax slab.");

            return Ok("Tax slab added successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("UpdateTaxSlab")]
    public async Task<IActionResult> UpdateTaxSlab([FromBody] TaxSlabDto dto)
    {
        var validationResult = await _taxSlabValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest($"Validation failed: {errors}");
        }

        try
        {
            var result = await _taxSlabService.UpdateTaxSlab(dto);
            if (result == null)
                return NotFound($"No tax slab found with ID: {dto.IdTaxSlab}");

            return Ok("Tax slab updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region ChildTaxThresholds
    [HttpGet("GetChildTaxThresholdList")]
    public async Task<IActionResult> GetChildTaxThresholdList()
    {
        try
        {
            var childTaxThresholdList = await _childTaxservice.GetAllChildTaxThresholds();

            if (!childTaxThresholdList.Any())
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("No child tax thresholds found."));
            }

            return Ok(ApiResponseDto<IEnumerable<ChildTaxThresholdDto>>.CreateSuccess(childTaxThresholdList, "Child tax thresholds retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("GetChildTaxThresholdByID")]
    public async Task<IActionResult> GetChildTaxThresholdByID(int id)
    {
        try
        {
            var childTaxThreshold = await _childTaxservice.GetChildTaxThresholdById(id);

            if (childTaxThreshold == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("No child tax threshold found."));
            }

            return Ok(ApiResponseDto<ChildTaxThresholdDto>.CreateSuccess(childTaxThreshold, "Child tax threshold retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("AddChildTaxThreshold")]
    public async Task<IActionResult> AddChildTaxThreshold([FromBody] ChildTaxThresholdDto dto)
    {
        var validationResult = await _childTaxvalidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        var existingThreshold = (await _childTaxservice.GetAllChildTaxThresholds())
            .FirstOrDefault(t => t.IdTaxConfig == dto.IdTaxConfig && t.ChildrenCount == dto.ChildrenCount);

        if (existingThreshold != null)
        {
            return Conflict(ApiResponseDto<string>.CreateFailure("Child tax threshold already exists."));
        }

        try
        {
            var result = await _childTaxservice.AddChildTaxThreshold(dto);
            if (result == null)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add child tax threshold."));
            }

            return Ok(ApiResponseDto<string>.CreateSuccess("Child tax threshold added successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("UpdateChildTaxThreshold")]
    public async Task<IActionResult> UpdateChildTaxThreshold([FromBody] ChildTaxThresholdDto dto)
    {
        var validationResult = await _childTaxvalidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
        }

        if (dto.IdChildTaxThreshold <= 0)
        {
            return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a child tax threshold."));
        }

        try
        {
            var existingThreshold = await _childTaxservice.GetChildTaxThresholdById(dto.IdChildTaxThreshold);
            if (existingThreshold == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("Child tax threshold not found."));
            }

            var conflictThreshold = (await _childTaxservice.GetAllChildTaxThresholds())
                .FirstOrDefault(t => t.IdTaxConfig == dto.IdTaxConfig && t.ChildrenCount == dto.ChildrenCount && t.IdChildTaxThreshold != dto.IdChildTaxThreshold);

            if (conflictThreshold != null)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure("Child tax threshold conflicts with an existing entry."));
            }

            var result = await _childTaxservice.UpdateChildTaxThreshold(dto);
            if (result == null)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update child tax threshold."));
            }

            return Ok(ApiResponseDto<string>.CreateSuccess("Child tax threshold updated successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        }
    }
    #endregion

}
