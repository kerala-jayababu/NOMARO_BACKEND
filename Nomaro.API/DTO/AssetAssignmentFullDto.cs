namespace Nomaro.API.DTO
{
    public class AssetAssignmentFullDto
    {
        public int IdAssetAssignment { get; set; }

        public int IdAsset { get; set; }
        public int IdEmployee { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? AssignedTillDate { get; set; }

        public string? Remarks { get; set; }
        public int AssignedBy { get; set; }
        public DateTime AssignedDateTime { get; set; }

        // ✅ Asset Details
        public string? AssetSerialNumber { get; set; }
        public string? AssetDetails { get; set; }
        public decimal AverageCost { get; set; }
        public string? AssetWorkingStatus { get; set; }
        public bool IsAllocated { get; set; }
        public int? DefaultDurationOfAssignment { get; set; }

        // ✅ Asset Type Details
        public int IdAssetType { get; set; }
        public string? AssetTypeName { get; set; }
    }
}

