using Asp.Versioning;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nomaro.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        // Called by the frontend before login to resolve company name, currency, date format, etc.
        [AllowAnonymous]
        [HttpGet("{companyCode}")]
        public async Task<IActionResult> GetByCompanyCode(string companyCode)
        {
            var config = await _tenantService.GetTenantConfigDtoAsync(companyCode);

            if (config == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure($"Company code '{companyCode}' was not found."));
            }

            return Ok(ApiResponseDto<TenantConfigDto>.CreateSuccess(config, "Company configuration retrieved successfully."));
        }
    }
}
