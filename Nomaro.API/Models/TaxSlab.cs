using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    /// <summary>Income tax slab of a tax year configuration (financial year + regime) and age category.</summary>
    public class TaxSlab
    {
        [Key]
        public int IdTaxSlab { get; set; }
        public int IdTaxYearConfig { get; set; }
        public string AgeCategory { get; set; }
        public decimal IncomeFrom { get; set; }
        /// <summary>NULL for the top slab ("and above").</summary>
        public decimal? IncomeTo { get; set; }
        public decimal TaxRate { get; set; }
    }
}
