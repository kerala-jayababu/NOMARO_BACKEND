using System;
using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class AssetAssignmentDto
    {
        [Key]
        public int IdAssetAssignment { get; set; }
        public int IdAsset { get; set; }
        public int IdEmployee { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? AssignedTillDate { get; set; }
        public int AssignedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
