using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ExitCases
    {
        [Key]
        public int IdExitCase { get; set; }

        [Required]
        [StringLength(20)]
        public string? CaseNumber { get; set; }

        [Required]
        public int IdEmployee { get; set; }

        [Required]
        public int IdExitType { get; set; }

        [Required]
        public int IdExitReason { get; set; }

        [Required]
        public DateTime InitiationDate { get; set; }

        [StringLength(2000)]
        public string? EmployeeReasonDetails { get; set; }

        [Required]
        public DateTime ProposedLWD { get; set; }

        public DateTime? ApprovedLWD { get; set; }

        public int? IdNoticePolicy { get; set; }

        public int? PolicyNoticeDays { get; set; }

        public bool? IsNoticeOverridden { get; set; }

        public int? EffectiveNoticeDays { get; set; }

        public DateTime? EarliestLWD { get; set; }

        [StringLength(2000)]
        public string? HandoverPlan { get; set; }

        public DateTime? ExitInterviewDate { get; set; }

        [StringLength(200)]
        public string? ContactAfterExit { get; set; }

        [Required]
        [StringLength(30)]
        public string? ExitStatus { get; set; }

        [StringLength(50)]
        public string? PendingWith { get; set; }

        public int? IdClearanceTemplate { get; set; }

        public int? AssignedClearanceTemplateBy { get; set; }

        public DateTime? ClearanceInitiatedOn { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? PendingWithIDEmployee { get; set; }
    }
}
