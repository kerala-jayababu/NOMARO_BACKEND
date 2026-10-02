using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class TaxYearConfigDto
    {
        public int IdTaxYearConfig { get; set; }
        public int IdFinancialYear { get; set; }
        public int IdTaxRegime { get; set; }
        public decimal StandardDeduction { get; set; }
        public decimal? RebateIncomeLimit { get; set; }
        public decimal? RebateMaxAmount { get; set; }
        public decimal CessRate { get; set; }
        public bool AllowMarginalRelief { get; set; }

        [NotMapped]
        public string? FinancialYear { get; set; }
        [NotMapped]
        public string? FinancialYearName { get; set; }
        [NotMapped]
        public DateTime? FinancialYearFrom { get; set; }
        [NotMapped]
        public DateTime? FinancialYearTo { get; set; }
        [NotMapped]
        public int SlabCount { get; set; }
    }
}
