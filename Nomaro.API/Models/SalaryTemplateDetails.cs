using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class SalaryTemplateDetails
    {
        [Key]
        public int? IdSalaryTemplateDetail { get; set; }
        public int? IdSalaryTemplate { get; set; }
        public int? IdSalaryHead { get; set; }
        public string? CalculationMethod { get; set; }
        public decimal? FixedAmount { get; set; }
        public int? PercentageOfIdSalaryHead { get; set; }
        public decimal? PercentageValue { get; set; }
        public string? CustomFormula { get; set; }
        public decimal? FinalSalaryAmount { get; set; }
        public string? Remarks { get; set; }
    }
}

