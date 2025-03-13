using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class OvertimeTransactionEntity
    {
        [Key]
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int IdOvertimeType { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime StartDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DurationInHours { get; set; }
        public string? ReasonForOverTime { get; set; } = string.Empty;
        public string? Attachment { get; set; } = string.Empty;
        public string? AttachmentDescription { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? DayType { get; set; }
        
    }
}
