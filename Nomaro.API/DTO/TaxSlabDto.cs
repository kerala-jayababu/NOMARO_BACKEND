using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class TaxSlabDto
    {
        public int IdTaxSlab { get; set; }     
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int? IdFinancialYear { get; set; }

        [NotMapped]
        public DateTime? FinancialYearFrom { get; set; }
        [NotMapped] 
        public DateTime? FinancialYearTo { get; set; }
    }
}

