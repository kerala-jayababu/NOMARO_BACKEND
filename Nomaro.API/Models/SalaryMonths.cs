using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class SalaryMonths
    {
        [Key]
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public DateTime SalaryMonthDate { get; set; }
        public string? SalaryGenerationStatus { get; set; }
        public DateTime SalaryMonthFrom { get; set; }
        public DateTime SalaryMonthTo { get; set; }

    }
}

