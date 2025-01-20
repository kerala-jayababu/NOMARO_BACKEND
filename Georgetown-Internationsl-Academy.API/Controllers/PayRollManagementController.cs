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
    public class PayRollManagementController : ControllerBase
    {
        private readonly ICurrencyConversionService _currencyConversionService;
        private readonly IValidator<CurrencyConversionDto> _currencyConversionValidator;
        public PayRollManagementController(ICurrencyConversionService currencyConversionService, IValidator<CurrencyConversionDto> currencyConversionValidator)
        {
            _currencyConversionService = currencyConversionService;
            _currencyConversionValidator = currencyConversionValidator;
        }

        [HttpGet("GetAllCurrencyConversions")]
        public async Task<IActionResult> GetAllCurrencyConversions()
        {
            try
            {
                var conversions = await _currencyConversionService.GetAllCurrencyConversions();

                if (!conversions.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No currency conversions found."));

                return Ok(ApiResponseDto<IEnumerable<CurrencyConversionDto>>.CreateSuccess(conversions, "Currency conversions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetCurrencyConversionById")]
        public async Task<IActionResult> GetCurrencyConversionById(int id)
        {
            try
            {
                var conversion = await _currencyConversionService.GetCurrencyConversionById(id);

                if (conversion == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Currency conversion not found."));

                return Ok(ApiResponseDto<CurrencyConversionDto>.CreateSuccess(conversion, "Currency conversion retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddCurrencyConversion")]
        public async Task<IActionResult> AddCurrencyConversion([FromBody] CurrencyConversionDto dto)
        {
            var validationResult = await _currencyConversionValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _currencyConversionService.AddCurrencyConversion(dto);

                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create currency conversion."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Currency conversion created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateCurrencyConversion")]
        public async Task<IActionResult> UpdateCurrencyConversion([FromBody] CurrencyConversionDto dto)
        {
            var validationResult = await _currencyConversionValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdCurrencyConversion <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0."));

            try
            {
                var result = await _currencyConversionService.UpdateCurrencyConversion(dto);

                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update currency conversion."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Currency conversion updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }





    }
}
