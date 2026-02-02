using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Models
{
    [Keyless]
    public class EmployeeHierarchyView
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public int? ReportingTo { get; set; }
        public int LevelNumber { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? EmployeePhotoFilePath { get; set; }
    }
}
