namespace Nomaro.API.DTO
{
    public class OvertimeTransactionFullDto
    {
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int IdOvertimeType { get; set; }
        public string? DayType { get; set; }
        public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationInHours { get; set; }
        public string? ReasonForOvertime { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentDescription { get; set; }
        public string? ApprovalStatus { get; set; }

        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }

        public int IdDepartment { get; set; }
        public int IdDesignation { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public int? IdSalaryMonthAccounted { get; set; }
        public decimal? SalaryAccountedAmount { get; set; }
        public List<ApprovalCycleDto> ApprovalCycles { get; set; } = new();

    }

    public class ApprovalCycleDto
    {
        public int IdWorkFlowConfigDetail { get; set; }
        public int IdOvertimeTransaction { get; set; }
        public int IdWorkFlowConfig { get; set; }
        public int LevelNumber { get; set; }

        public string ApprovalAuthorityType { get; set; } = "";
        public int? ApprovalAuthorityID { get; set; }
        public string? ApprovalStatusName { get; set; }

        // Expected approver (from WCD rule)
        public string? ApprovalAuthorityName { get; set; }
        public string? ApprovalAuthorityIdEmployees { get; set; }
        // Actual action overlay (from allocations)
        public string ApprovalStatus { get; set; } = "Pending";   // you showed JB/SK/KI or Pending
        public DateTime? ActionDate { get; set; }
        public int? ActionedById { get; set; }
        public string? ActionedByName { get; set; }
    }
}

