using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class DocumentTypeDto
    {
        [Key]
        public int IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
    }
}
