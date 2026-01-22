using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    [Table("ExitCaseClearanceAssignments")]
    public class ExitCaseClearanceAssignment
    {
        [Key]
        public int IdExitCaseClearanceAssignment { get; set; }

        [Required]
        public int IdExitCase { get; set; }

        [Required]
        public int IdDepartment { get; set; }

        [Required]
        public int IdClearanceTemplate { get; set; }

        [Required]
        public int IdAssigneeUser { get; set; }

        [Required]
        [StringLength(20)]
        public string DeptClearanceStatus { get; set; } = null!;

        [Required]
        public int AssignedBy { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; }
    }
}
