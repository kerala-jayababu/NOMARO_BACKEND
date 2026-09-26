using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class AssetTypes
    {
        [Key]
        public int IdAssetType { get; set; }
        public string? AssetTypeName { get; set; }
    }
}
