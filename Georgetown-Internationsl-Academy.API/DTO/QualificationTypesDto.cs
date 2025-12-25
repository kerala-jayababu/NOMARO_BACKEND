using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class QualificationTypesDto
    {
        [Key]
        public int IdQualificationType { get; set; }
        public string? QualificationTypeName { get; set; }
    }
}
