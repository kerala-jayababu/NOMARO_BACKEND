using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    /// <summary>Income tax settings of a financial year for one tax regime.</summary>
    public class TaxYearConfigs
    {
        [Key]
        public int IdTaxYearConfig { get; set; }
        /// <summary>Text form of the year (e.g. "2026-27"); kept filled while the column exists. IdFinancialYear is the link.</summary>
        public string FinancialYear { get; set; }
        public int? IdFinancialYear { get; set; }
        public int IdTaxRegime { get; set; }
        public decimal StandardDeduction { get; set; }
        public decimal? RebateIncomeLimit { get; set; }
        public decimal? RebateMaxAmount { get; set; }
        public decimal CessRate { get; set; }
        public bool AllowMarginalRelief { get; set; }
    }
}
