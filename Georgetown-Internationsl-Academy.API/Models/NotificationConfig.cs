using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class NotificationConfig
    {
        [Key]
        public int IdNotificationConfig { get; set; }
        public string? EntityCode {  get; set; }
        public string NotificationType { get; set; }
        public string? NotificationTypeDesc {  get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailContent { get; set; }
        public string? AppNotificationText { get; set; }
        public string? WebLink { get; set; }
        public string? LogoText { get; set; }
        public bool? EmailNotificationEnabled { get; set; }
        public bool? AppNotificationEnabled { get; set; }
        public int? LevelNumber { get; set; }
        
    }

}
