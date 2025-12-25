namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeExperienceGetDto
    {
        public int IdEmployeeExperience { get; set; }
        public int IdEmployee { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public int? IdCountry { get; set; }
        public string? Designation { get; set; }
        public string? ReasonForLeaving { get; set; }
        public decimal? LastDrawnSalary { get; set; }
        public decimal ExperienceInYears { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // ✅ File info
        public string? ExperienceCertificatePath { get; set; }
        public string? CertificateFileName { get; set; }

        // ✅ File binary (JSON will return base64)
        public byte[]? CertificateBinary { get; set; }
    }
}
