using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class NotificationDto
    {
        public int IdNotification { get; set; }

        public int? IdNotificationConfig { get; set; }

        public string? NotificationType { get; set; }

        public int? SentByIdEmployee { get; set; }

        public int? ReceivedByIdEmployee { get; set; }

        public string? EmailSubject { get; set; }

        public string? EmailContent { get; set; }

        public string? EmailSentStatus { get; set; }

        public string? AppNotificationText { get; set; }

        public string? NotificationLink { get; set; }

        public bool? IsReadAppNotification { get; set; }    

        public string? Status { get; set; }
        public string? LogoText { get; set; }

        public int? RelatedRecordID { get; set; }

        public string? RelatedRecordType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
