using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class QualificationTypesDto
    {
        [Key]
        public int IdQualificationType { get; set; }
        public string? QualificationTypeName { get; set; }
    }
}

