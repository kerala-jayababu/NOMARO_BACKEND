using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeDocumentDto
    {
        [Key]
        public int IdEmployeeDocument { get; set; }
        public int IdEmployee { get; set; }

        public int IdDocumentType { get; set; }
        public string? DocumentTypeName { get; set; }

        public string? Remarks { get; set; }
        public string? DocumentFilePath { get; set; }

        // ✅ Only when IdEmployeeDocument is specified
        public string? FileName { get; set; }
        public byte[]? DocumentBinary { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}