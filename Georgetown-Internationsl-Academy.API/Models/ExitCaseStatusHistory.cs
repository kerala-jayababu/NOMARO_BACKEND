using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    [Table("ExitCaseStatusHistory")]
    public class ExitCaseStatusHistory
    {
        [Key]
        public int IdExitCaseStatusHistory { get; set; }

        [Required]
        public int IdExitCase { get; set; }

        [Required]
        [StringLength(40)]
        public string ActionType { get; set; } = null!;

        [StringLength(30)]
        public string? FromStatus { get; set; }

        [Required]
        [StringLength(30)]
        public string ToStatus { get; set; } = null!;

        [StringLength(50)]
        public string? PendingWith { get; set; }

        [StringLength(2000)]
        public string? Remarks { get; set; }

        public DateTime? ApprovedLWD { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int OrderNumber { get; set; }
    }
}
