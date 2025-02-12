using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
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
    private readonly IChildTaxThresholdService _childTaxservice;
    private readonly IValidator<ChildTaxThresholdDto> _childTaxvalidator;
    public TaxConfigsController( ITaxSlabService taxSlabService,
        IValidator<TaxSlabDto> taxSlabValidator, IChildTaxThresholdService childTaxservice, IValidator<ChildTaxThresholdDto> childTaxvalidator)
    {
   
        _taxSlabService = taxSlabService;
        _taxSlabValidator = taxSlabValidator;
        _childTaxservice = childTaxservice;
        _childTaxvalidator = childTaxvalidator;
    }


    #region TaxSlabs
    [HttpGet("GetAllTaxSlabs")]
    public async Task<IActionResult> GetAllTaxSlabs(int? idFinancialYear = null)
    {
        try
        {
            var taxSlabs = await _taxSlabService.GetAllTaxSlabs(idFinancialYear);
            if (!taxSlabs.Any())
                return NotFound(ApiResponseDto<string>.CreateFailure("No tax slabs found."));

            return Ok(ApiResponseDto<IEnumerable<TaxSlabDto>>.CreateSuccess(taxSlabs, " taxSlabs retrieved successfully."));
          
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
                return NotFound(ApiResponseDto<string>.CreateFailure("taxSlabs not found."));
            }


            return Ok(ApiResponseDto<TaxSlabDto>.CreateSuccess(taxSlab, "taxSlabs retrieved successfully."));    
            
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

        try
        {
            var result = await _taxSlabService.AddTaxSlab(dto);
            if (result == null)
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add tax slab."));


            return Ok(ApiResponseDto<string>.CreateSuccess("tax slab added successfully."));
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
            return BadRequest(ApiResponseDto<IEnumerable<string>>.CreateFailure(errors));
            
        }

        try
        {
            var result = await _taxSlabService.UpdateTaxSlab(dto);
            if (result == null)
                return NotFound(ApiResponseDto<string>.CreateFailure($"No tax slab found with ID: {dto.IdTaxSlab}"));


            return Ok(ApiResponseDto<TaxSlabDto>.CreateSuccess(result, "Tax slab updated successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region ChildTaxThresholds
    [HttpGet("GetChildTaxThresholdList")]
    public async Task<IActionResult> GetChildTaxThresholdList(int? idFinancialYear = null)
    {
        try
        {
            var childTaxThresholdList = await _childTaxservice.GetAllChildTaxThresholds(idFinancialYear);

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
