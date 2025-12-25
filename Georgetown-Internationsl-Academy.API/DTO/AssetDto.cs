using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class AssetDto
    {
        [Key]
        public int IdAsset { get; set; }
        public int IdAssetType { get; set; }
        public string? AssetSerialNumber { get; set; }
        public string? AssetDetails { get; set; }
        public decimal AverageCost { get; set; }
        public string? AssetWorkingStatus { get; set; }
        public bool IsAllocated { get; set; }
    }
}