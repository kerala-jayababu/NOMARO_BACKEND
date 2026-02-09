using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class MobileNotificationPostDto
    {
        [Key]
        public int IdMobileNotification { get; set; }
        public int IdEmployee { get; set; }
        public string NotificationType { get; set; } = "";
        public string NotificationMessage { get; set; } = "";
        public int? EntityTablePrimaryKeyID { get; set; }

    }

}
