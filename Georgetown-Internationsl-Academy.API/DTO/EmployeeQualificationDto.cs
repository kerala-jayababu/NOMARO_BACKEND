using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeQualificationDto
    {
        [Key]
        public int? IdEmployeeQualification { get; set; }
        public int IdEmployee { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public int IdQualificationType { get; set; }
        public string? QualificationTypeName { get; set; }

        public string? QualificationName { get; set; }
        public int YearOfCompletion { get; set; }
        public string? GradeOrPercentage { get; set; }
        public string? Specialization { get; set; }
        public string? InstitutionName { get; set; }
        public int IdCountry { get; set; }

        // ✅ Certificate details
        public string? CertificateDocumentPath { get; set; }
        public string? CertificateFileName { get; set; }
        public byte[]? CertificateBinary { get; set; }

        public IFormFile? CertificateDocument { get; set; }
    }
}