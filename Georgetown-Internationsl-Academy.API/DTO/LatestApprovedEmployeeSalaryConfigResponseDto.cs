namespace Georgetown_Internationsl_Academy.API.DTO
{
    /// <summary>
    /// Latest approved salary configuration for an employee, including line items and approval timeline.
    /// </summary>
    public class LatestApprovedEmployeeSalaryConfigResponseDto
    {
        public EmployeeSalaryConfigDto? Config { get; set; }

        /// <summary>Ordered timeline: configuration created, submitted for approval, then each approval level (latest cycle only).</summary>
        public List<SalaryConfigApprovalTimelineStepDto> ApprovalWorkflowSteps { get; set; } = new();
    }
}
