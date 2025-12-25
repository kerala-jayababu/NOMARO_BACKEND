using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    //[Authorize]
    public class AssetController : ControllerBase
    {
        private readonly IAssetServices _assetService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public AssetController(
            IAssetServices assetService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService)
        {
            _assetService = assetService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        #region Asset Types

        [HttpGet("GetAssetTypes")]
        public async Task<IActionResult> GetAssetTypes()
        {
            try
            {
                var assetTypes = await _assetService.GetAssetTypes();

                if (!assetTypes.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<AssetTypeDto>>
                        .CreateSuccess(Enumerable.Empty<AssetTypeDto>(), "No asset types available."));
                }

                return Ok(ApiResponseDto<IEnumerable<AssetTypeDto>>
                    .CreateSuccess(assetTypes, "Asset types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateAssetTypes")]
        public async Task<IActionResult> AddOrUpdateAssetTypes(List<AssetTypeDto> assetTypeDtos)
        {
            if (assetTypeDtos == null || !assetTypeDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of asset types."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:AssetTypes"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _assetService.AddOrUpdateAssetTypes(assetTypeDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update asset types."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Asset types added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Assets

        [HttpGet("GetAssets")]
        public async Task<IActionResult> GetAssets()
        {
            try
            {
                var assets = await _assetService.GetAssets();

                if (!assets.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<AssetDto>>
                        .CreateSuccess(Enumerable.Empty<AssetDto>(), "No assets available."));
                }

                return Ok(ApiResponseDto<IEnumerable<AssetDto>>
                    .CreateSuccess(assets, "Assets retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateAssets")]
        public async Task<IActionResult> AddOrUpdateAssets(List<AssetDto> assetDtos)
        {
            if (assetDtos == null || !assetDtos.Any())
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure("Invalid input. Please provide a valid list of assets."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:Assets"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _assetService.AddOrUpdateAssets(assetDtos);

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update assets."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Assets added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion
    }
}
