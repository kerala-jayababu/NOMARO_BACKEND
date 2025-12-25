using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeQualifications
    {
        [Key]
        public int IdEmployeeQualification { get; set; }
        public int IdEmployee { get; set; }
        public int IdQualificationType { get; set; }
        [ForeignKey("IdQualificationType")]
        public QualificationTypes? QualificationType { get; set; }
        public string? QualificationName { get; set; }
        public string? Specialization { get; set; }
        public string? InstitutionName { get; set; }
        public int IdCountry { get; set; }
        public int YearOfCompletion { get; set; }
        public string? GradeOrPercentage { get; set; }
        public string? CertificateDocumentPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

    }
}