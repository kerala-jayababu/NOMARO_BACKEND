using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class OvertimeTransactionEntity
    {
        [Key]
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int IdOvertimeType { get; set; }
        public string OvertimeTypeName { get; set; } = string.Empty;
        public DateTime OvertimeDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public decimal DurationInHours { get; set; }
        public string ReasonForOverTime { get; set; } = string.Empty;
        public byte[]? Attachment { get; set; }
        public string AttachmentDescription { get; set; } = string.Empty;
        public DateTime? ManagerApprovedDate { get; set; }
        public DateTime? HRApprovedDate { get; set; }
    }
}
