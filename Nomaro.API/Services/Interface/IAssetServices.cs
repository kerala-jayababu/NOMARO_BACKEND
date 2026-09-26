using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IAssetServices
    {
        Task<IEnumerable<AssetTypeDto>> GetAssetTypes();
        Task<bool> AddOrUpdateAssetTypes(List<AssetTypeDto> assetTypeDtos);

        Task<IEnumerable<AssetDto>> GetAssets(string? searchText);
        Task<bool> AddOrUpdateAssets(List<AssetDto> assetDtos);
    }
}

