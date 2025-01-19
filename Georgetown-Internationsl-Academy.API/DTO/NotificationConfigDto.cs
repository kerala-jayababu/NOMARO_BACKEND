namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class NotificationConfigDto
    {
        public int IdNotificationConfig { get; set; }
        public string NotificationType { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailContent { get; set; }
        public string? AppNotificationText { get; set; }
        public string? WebLink { get; set; }
    }
}
