namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class OvertimeTransactionFullDto
    {
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int IdOvertimeType { get; set; }

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

        public List<ApprovalCycleDto> ApprovalCycles { get; set; } = new();
    }
    public class ApprovalCycleDto
    {
        public int IdApprovalWorkFlow { get; set; }
        public int IdWorkFlowConfig { get; set; }

        public string EntityCode { get; set; } = "";
        public int EntityTablePrimaryKeyID { get; set; }

        public int CycleIndex { get; set; }
        public int LevelNumber { get; set; }

        public int SourcedIdEmployee { get; set; }
        public string? SourcedEmployeeName { get; set; }

        public DateTime SentDate { get; set; }

        public string TargetIdEmployee { get; set; } = "";
        public string? TargetEmployeeName { get; set; }

        public string? ExpectedApprovalActionStatus { get; set; }
        public string? ActionStatus { get; set; }

        public int? ActionedBy { get; set; }
        public string? ActionedByName { get; set; }

        public DateTime? ActionDate { get; set; }
        public string? RejectionRemarks { get; set; }
    }
}
