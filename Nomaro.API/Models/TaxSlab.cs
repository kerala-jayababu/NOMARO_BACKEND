using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class TaxSlab
    {
        [Key]
        public int IdTaxSlab { get; set; }       
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int? IdFinancialYear { get; set; }
    }
}

