namespace Georgetown_Internationsl_Academy.API.DTO
{
    /// <summary>
    /// One entry in the salary config approval story: who created, who submitted, who approved at each level.
    /// </summary>
    public class SalaryConfigApprovalTimelineStepDto
    {
        /// <summary>CREATED, SUBMITTED, or APPROVED.</summary>
        public string EventKind { get; set; } = string.Empty;

        /// <summary>UI label, e.g. Configuration created, Submitted for approval, APPROVED / INTERIM APPROVED.</summary>
        public string DisplayLabel { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public int? ActorEmployeeId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorDesignationName { get; set; }

        /// <summary>Workflow level when EventKind is APPROVED; null for CREATED / SUBMITTED.</summary>
        public int? WorkflowLevelNumber { get; set; }

        public int? IdApprovalWorkFlow { get; set; }
    }
}
