using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class ExitReasons
    {
        [Key]
        public int IdExitReason { get; set; }
        public string ReasonCode { get; set; }
        public string ReasonName { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
