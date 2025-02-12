using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
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
