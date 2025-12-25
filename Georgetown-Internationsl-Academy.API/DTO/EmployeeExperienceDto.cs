using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeExperienceDto
    {
        [Key]
        public int IdEmployeeExperience { get; set; }
        public int IdEmployee { get; set; }
        public string? CompanyName { get; set; }
        public string? Designation { get; set; }
        public decimal? LastDrawnSalary { get; set; }
        public decimal ExperienceInYears { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int CreatedBy { get; set; }
    }
}