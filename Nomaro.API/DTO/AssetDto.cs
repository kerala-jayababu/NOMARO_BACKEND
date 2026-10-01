using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
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

        [NotMapped]
        public string? AssetTypeName { get; set; }

        // Current allocation (AssetAssignments). Both optional: an asset is allocated to an office,
        // and to an employee of that office only when chosen explicitly.
        public int? IdOffice { get; set; }
        public int? IdEmployee { get; set; }
        [NotMapped]
        public string? OfficeName { get; set; }
        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
    }
}
