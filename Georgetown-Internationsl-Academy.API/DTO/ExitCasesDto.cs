using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ExitCasesDto
    {
        public int IdExitCase { get; set; }

        public string? CaseNumber { get; set; }

        public int IdEmployee { get; set; }

        public int IdExitType { get; set; }

        public int IdExitReason { get; set; }

        public DateTime InitiationDate { get; set; }

        public string? EmployeeReasonDetails { get; set; }

        public DateTime ProposedLWD { get; set; }

        public DateTime? ApprovedLWD { get; set; }

        public int? IdNoticePolicy { get; set; }

        public int? PolicyNoticeDays { get; set; }

        public bool? IsNoticeOverridden { get; set; }

        public int? EffectiveNoticeDays { get; set; }

        public DateTime? EarliestLWD { get; set; }

        public string? HandoverPlan { get; set; }

        public DateTime? ExitInterviewDate { get; set; }

        public string? ContactAfterExit { get; set; }

        public string? ExitStatus { get; set; }

        public string? PendingWith { get; set; }

        public int? IdClearanceTemplate { get; set; }

        public int? AssignedClearanceTemplateBy { get; set; }

        public DateTime? ClearanceInitiatedOn { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? PendingWithIDEmployee { get; set; }
    }

    /// <summary>
    /// DTO for submitting resignation by employee (Add or Update)
    /// </summary>
    public class SubmitResignationDto
    {
        // 0 = Add new, >0 = Update existing
        public int IdExitCase { get; set; } = 0;

        [Required(ErrorMessage = "Employee ID is required")]
        public int IdEmployee { get; set; }

        [Required(ErrorMessage = "Exit Reason is required")]
        public int IdExitReason { get; set; }

        [StringLength(2000)]
        public string? EmployeeReasonDetails { get; set; }

        [Required(ErrorMessage = "Proposed Last Working Day is required")]
        public DateTime ProposedLWD { get; set; }
    }

    /// <summary>
    /// DTO for response after resignation submission
    /// </summary>
    public class SubmitResignationResponseDto
    {
        public bool Success { get; set; }

        public int? IdExitCase { get; set; }

        public string? CaseNumber { get; set; }

        public string? Message { get; set; }

        public List<string>? Errors { get; set; }
    }
}
