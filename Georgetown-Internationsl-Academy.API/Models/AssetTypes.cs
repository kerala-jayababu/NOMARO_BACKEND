using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class AssetTypes
    {
        [Key]
        public int IdAssetType { get; set; }
        public string? AssetTypeName { get; set; }
    }
}