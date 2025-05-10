using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ReportsMaster
    {
        [Key]
        public int idReport { get; set; }

        [Required]
        [MaxLength(50)]
        public string? ReportName { get; set; }
        public int OrderNumber { get; set; }
        public int idParentReport { get; set; }

        [MaxLength(100)]
        public string? StoredProcName { get; set; }

        [MaxLength(200)]
        public string? ReportTitle { get; set; }

        [MaxLength(50)]
        public string? PrintOrientation { get; set; }

        public int? RowsInaPage { get; set; }
        public int? RowHeight { get; set; }

        public char RemoveColumnIfNoData { get; set; }
        public char IncludeSLNO { get; set; }
        public char ViewableAdminOnly { get; set; }

        public string? idPermissionEmployeesList { get; set; }

        public char Enabled { get; set; }

        [MaxLength(1000)]
        public string? MergeColumnDetails { get; set; }

        public bool HeaderRequired { get; set; }
    }
}
