using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class DocumentTypes
    {
        [Key]
        public int IdDocumentType { get; set; }

        public string? DocumentTypeName { get; set; }
    }
}