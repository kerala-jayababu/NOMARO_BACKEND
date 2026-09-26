using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class OrganizationHierarchyDto
    {
        [Key]
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public int? ReportingTo { get; set; }
        public int LevelNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? EmployeePhotoFilePath { get; set; }

        // ✅ REQUIRED
        public byte[]? EmployeePhoto { get; set; }
    }
}

