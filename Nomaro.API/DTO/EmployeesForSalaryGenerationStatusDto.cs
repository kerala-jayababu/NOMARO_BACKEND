using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeesForSalaryGenerationStatusDto
    {
        public int IdEmployee { get; set; }
        public int IdSalaryMonth { get; set; }
        public string? SalaryGenerationRemarks { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public string? SalaryMonthText { get; set; }
    }
}
