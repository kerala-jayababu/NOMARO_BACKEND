using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class ChildTaxThresholdDto
    {
        public int IdChildTaxThreshold { get; set; }      
        public int ChildrenCount { get; set; }
        public decimal TaxThresholdAmount { get; set; }
        public int? IdFinancialYear { get; set; }

        [NotMapped]
        public DateTime? FinancialYearFrom { get; set; }
        [NotMapped]
        public DateTime? FinancialYearTo { get; set; }
    }
}

