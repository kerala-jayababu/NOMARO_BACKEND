using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IAssetServices
    {
        Task<IEnumerable<AssetTypeDto>> GetAssetTypes();
        Task<bool> AddOrUpdateAssetTypes(List<AssetTypeDto> assetTypeDtos);

        Task<IEnumerable<AssetDto>> GetAssets();
        Task<bool> AddOrUpdateAssets(List<AssetDto> assetDtos);
    }
}
