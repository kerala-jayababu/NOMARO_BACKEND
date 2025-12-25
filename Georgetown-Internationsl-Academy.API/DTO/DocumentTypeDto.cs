using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class DocumentTypeDto
    {
        [Key]
        public int IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }
    }
}