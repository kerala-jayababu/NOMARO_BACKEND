using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class ChildTaxThreshold
    {
        [Key]
        public int IdChildTaxThreshold { get; set; }      
        public int ChildrenCount { get; set; }
        public decimal TaxThresholdAmount { get; set; }
        public int? IdFinancialYear { get; set; }
    }
}

