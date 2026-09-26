using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class Assets
    {
        [Key]
        public int IdAsset { get; set; }
        public int IdAssetType { get; set; }
        public string? AssetSerialNumber { get; set; }
        public string? AssetDetails { get; set; }
        public decimal AverageCost { get; set; }
        public string? AssetWorkingStatus { get; set; }
        public int? DefaultDurationOfAssignment { get; set; }
        public bool IsAllocated { get; set; }
        // ✅ Navigation Property
        [ForeignKey("IdAssetType")]
        public virtual AssetTypes AssetType { get; set; }
    }
}
