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

    /// <summary>
    /// DTO for getting resignation requests based on role and hierarchy
    /// </summary>
    public class ResignationRequestDto
    {
        // Exit Case Info
        public int IdExitCase { get; set; }
        public string? CaseNumber { get; set; }
        public DateTime InitiationDate { get; set; }
        public string? ExitStatus { get; set; }
        public string? PendingWith { get; set; }

        // Employee Info (Resigning Employee)
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public int? IdEmployeeDepartment { get; set; }
        public string? EmployeeDepartmentName { get; set; }
        public int? IdEmployeeDesignation { get; set; }
        public string? EmployeeDesignationName { get; set; }

        // Exit Type Info
        public int? IdExitType { get; set; }
        public string? ExitTypeCode { get; set; }
        public string? ExitTypeName { get; set; }

        // Exit Reason Info
        public int IdExitReason { get; set; }
        public string? ExitReasonCode { get; set; }
        public string? ExitReasonName { get; set; }
        public string? EmployeeReasonDetails { get; set; }
        public DateTime ProposedLWD { get; set; }
        public DateTime? ApprovedLWD { get; set; }

        // Notice Period Info
        public int? IdNoticePolicy { get; set; }
        public string? NoticePolicyCode { get; set; }
        public string? NoticePolicyName { get; set; }
        public int? PolicyNoticeDays { get; set; }
        public int? EffectiveNoticeDays { get; set; }
        public bool? IsNoticeOverridden { get; set; }

        // Clearance Info
        public int? IdClearanceTemplate { get; set; }
        public string? ClearanceTemplateName { get; set; }
        public string? ClearanceTemplateDescription { get; set; }
        public DateTime? ClearanceInitiatedOn { get; set; }

        // Reporting Officer Info
        public int? PendingWithIDEmployee { get; set; }
        public string? ReportingOfficerCode { get; set; }
        public string? ReportingOfficerName { get; set; }
        public int? ReportingOfficerDepartment { get; set; }
        public string? ReportingOfficerDepartmentName { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public List<ExitCaseStatusHistoryDto> ExitCaseHistories { get; set; } = new();
        public List<ExitCaseClearanceAssignmentDto> ClearanceAssignments { get; set; } = new();
        public List<ExitCaseDepartmentClearanceLineDto> DepartmentClearanceLines { get; set; } = new();
    }
    public class ExitCaseDepartmentClearanceLineDto
    {
        public int IdExitCase { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public string CheckListItem { get; set; }    
        public string DeptClearanceStatus { get; set; }
        public int SortOrder { get; set; }
    }
    public class ExitCaseClearanceAssignmentDto
    {
        public int IdExitCase { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public int IdAssigneeUser { get; set; }
        public string DeptClearanceStatus { get; set; }
        public DateTime AssignedAt { get; set; }
    }
    /// <summary>
    /// DTO for Reporting Officer to approve/reject resignation and pass to HR
    /// </summary>
    /// 
    public class SubmitReportingOfficerActionsDto
    {
        [Required(ErrorMessage = "IdExitCase is required")]
        public int IdExitCase { get; set; }

        [Required(ErrorMessage = "IdEmployee is required")]
        public int IdEmployee { get; set; }

        public DateTime? ApprovedLWD { get; set; } // Required only for Approved action

        public string? HandOverNotes { get; set; }

        [Required(ErrorMessage = "Action is required (Approved/Rejected)")]
        public string? Action { get; set; } // "Approved" or "Rejected"

        public string? Remarks { get; set; }
    }
    public class ExitCaseStatusHistoryDto
    {
        public int IdExitCase { get; set; }
        public string ActionType { get; set; }
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public string PendingWith { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response DTO for Reporting Officer Actions
    /// </summary>
    public class ReportingOfficerActionResponseDto
    {
        public bool Success { get; set; }
        public int IdExitCase { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
