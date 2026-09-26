using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class ClearanceTemplates
    {
        [Key]
        public int IdClearanceTemplate { get; set; }
        public string TemplateName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
