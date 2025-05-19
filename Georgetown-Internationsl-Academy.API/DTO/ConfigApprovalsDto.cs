namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ConfigApprovalsDto
    {
        public int IdApprovalWorkFlow { get; set; }
        public string? EntityName { get; set; }
        public int EntityTablePrimaryKeyID { get; set; }
        public string? EntityCode { get; set; }
        public string? Details { get; set; }
        public int CycleIndex { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime SentDate { get; set; }
        public string? TargetIdEmployee { get; set; }
        public string? ActionStatus { get; set; }
        public string? CurrentStatus { get; set; }
        public string? RejectionRemarks { get; set; }
    }
}
