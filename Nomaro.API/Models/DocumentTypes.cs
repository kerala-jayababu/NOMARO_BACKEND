using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class DocumentTypes
    {
        [Key]
        public int IdDocumentType { get; set; }

        public string? DocumentTypeName { get; set; }
    }
}
