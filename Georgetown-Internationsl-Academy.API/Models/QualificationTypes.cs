using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class QualificationTypes
    {
        [Key]
        public int IdQualificationType { get; set; }
        public string? QualificationTypeName { get; set; }
    }
}
