using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ReportsMasterDto
    {
        [Key]
        public int idReport { get; set; }
        public string? ReportName { get; set; }
        public int OrderNumber { get; set; }
        public int idParentReport { get; set; }
        public string? StoredProcName { get; set; }
        public string? ReportTitle { get; set; }
        public string? PrintOrientation { get; set; }
        public char RemoveColumnIfNoData { get; set; }
        public char IncludeSLNO { get; set; }
        public char ViewableAdminOnly { get; set; }
        public string? idPermissionEmployeesList { get; set; }
        public string? MergeColumnDetails { get; set; }
        public int? RowsInaPage { get; set; }
        public int? RowHeight { get; set; }
        public char Enabled { get; set; }
    }
}
