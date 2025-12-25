using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeExperiences
    {
        [Key]
        public int IdEmployeeExperience { get; set; }
        public int IdEmployee { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public int? IdCountry { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal? LastDrawnSalary { get; set; }
        public string? ReasonForLeaving { get; set; }
        public decimal ExperienceInYears { get; set; }
        public string? ExperienceCertificatePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}