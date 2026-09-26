using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    [Table("ExitCaseDepartmentClearanceLines")]
    public class ExitCaseDepartmentClearanceLine
    {
        [Key]
        public int IdExitCaseDepartmentClearanceLine { get; set; }

        [Required]
        public int IdExitCase { get; set; }

        [Required]
        public int IdDepartment { get; set; }

        public int? IdTemplateDept { get; set; }

        [StringLength(200)]
        public string? CheckListItem { get; set; }

        [StringLength(20)]
        public string? DeptClearanceStatus { get; set; }

        [StringLength(1000)]
        public string? DeptRemarks { get; set; }

        public int? ClearedBy { get; set; }

        public DateTime? ClearedAt { get; set; }

        public int? SortOrder { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}

