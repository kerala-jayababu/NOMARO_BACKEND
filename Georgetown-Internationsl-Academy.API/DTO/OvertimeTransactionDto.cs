namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class OvertimeTransactionDto
    {
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int IdOvertimeType { get; set; }
        public string OvertimeTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationInHours { get; set; }
        public string ReasonForOvertime { get; set; } = string.Empty;
        public string? Attachment { get; set; }
        public string? AttachmentDescription { get; set; }
        public int? IdManagerApprovedBy { get; set; }
        public DateTime? ManagerApprovedDate { get; set; }
        public int? IdHRApprovedBy { get; set; }
        public DateTime? HRApprovedDate { get; set; }
    }
}
