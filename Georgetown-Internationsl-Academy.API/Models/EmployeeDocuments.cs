using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeDocuments
    {
        [Key]
        public int IdEmployeeDocument { get; set; }

        public int IdEmployee { get; set; }
        public int IdDocumentType { get; set; }
        public DateTime? DocumentValidTill { get; set; }
        public string? Remarks { get; set; }
        public string? DocumentFilePath { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}