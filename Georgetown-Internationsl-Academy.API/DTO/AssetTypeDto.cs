using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class AssetTypeDto
    {
        [Key]
        public int IdAssetType { get; set; }
        public string? AssetTypeName { get; set; }
    }
}