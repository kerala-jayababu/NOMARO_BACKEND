using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class EmployeeSalaryConfigDetails
    {
        [Key]
        public int? IdEmployeeSalaryConfigDetail { get; set; }
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdSalaryHead { get; set; }
        public decimal? FixedAmount { get; set; }
        public decimal? PercentageValue { get; set; }
        public decimal? SalaryAmount {  get; set; } 
    }
}

