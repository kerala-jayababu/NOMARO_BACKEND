using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class AssetTypeDto
    {
        [Key]
        public int IdAssetType { get; set; }
        public string? AssetTypeName { get; set; }
    }
}
