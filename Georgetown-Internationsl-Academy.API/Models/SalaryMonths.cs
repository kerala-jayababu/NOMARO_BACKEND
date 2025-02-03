using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class SalaryMonths
    {
        [Key]
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public DateTime SalaryMonthDate { get; set; }
        public string? SalaryGenerationStatus { get; set; }
    }
}
