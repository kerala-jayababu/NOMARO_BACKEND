using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class AuditLogs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAudit { get; set; }

        [Required]
        [MaxLength(20)]
        public string ActionType { get; set; }         // Create | Update | Delete

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; }         // e.g. Employee, LeaveApplication

        public int EntityId { get; set; }              // PK of the affected record

        [MaxLength(100)]
        public string ActionBy { get; set; }           // UserId from JWT Claims

        public DateTime ActionTimestamp { get; set; }  // UTC DateTime

        [Column(TypeName = "nvarchar(max)")]
        public string ActionDetails { get; set; }      // JSON of new/changed data

        [MaxLength(50)]
        public string IPAddress { get; set; }          // Client IP

        [MaxLength(500)]
        public string DeviceInfo { get; set; }         // User-Agent string
    }
}

