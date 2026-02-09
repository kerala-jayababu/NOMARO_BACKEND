using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class MobileNotifications
    {
        [Key]
        public int IdMobileNotification { get; set; }
        public string ViewType { get; set; } = "";   // EMP / ALL
        public int? IdEmployee { get; set; }          // Only for EMP
        public string NotificationType { get; set; } = "";
        public int? EntityTablePrimaryKeyID { get; set; }
        public string NotificationMessage { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public bool ReadStatus { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}
