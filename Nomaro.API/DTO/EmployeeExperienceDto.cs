using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class EmployeeExperienceDto
    {
        [Key]
        public int IdEmployeeExperience { get; set; }
        public int IdEmployee { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public int? IdCountry { get; set; }
        public string? Designation { get; set; }
        public string ReasonForLeaving { get; set; }        
        public decimal? LastDrawnSalary { get; set; }
        public decimal ExperienceInYears { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }        
        public IFormFile? ExperienceDocument { get; set; }
     
}
}
