using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class ScreenAttributeConfigController : ControllerBase
    {
        private readonly IScreenAttributeConfigService _screenAttributeConfigService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ScreenAttributeConfigController> _logger;

        public ScreenAttributeConfigController(
            IScreenAttributeConfigService screenAttributeConfigService,
            ITenantService tenantService,
            ILogger<ScreenAttributeConfigController> logger)
        {
            _screenAttributeConfigService = screenAttributeConfigService;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>Returns field display settings for a screen in the company resolved from the request URL.</summary>
        [HttpGet("GetByScreenCode")]
        [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ScreenAttributeConfigDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByScreenCode(
            [FromQuery] string screenCode,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(screenCode))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("A screen code is required."));
            }

            var companyCode = _tenantService.CompanyCode;
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("A company code is required."));
            }

            try
            {
                var configs = await _screenAttributeConfigService.GetByScreenCodeAsync(
                    companyCode,
                    screenCode,
                    cancellationToken);
                return Ok(ApiResponseDto<IReadOnlyList<ScreenAttributeConfigDto>>.CreateSuccess(
                    configs,
                    "Screen field configuration retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve screen field configuration for company {CompanyCode} and screen {ScreenCode}.",
                    companyCode,
                    screenCode);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateFailure("Failed to retrieve screen field configuration."));
            }
        }
    }
}
