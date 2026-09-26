using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class QualificationTypes
    {
        [Key]
        public int IdQualificationType { get; set; }
        public string? QualificationTypeName { get; set; }
    }
}

