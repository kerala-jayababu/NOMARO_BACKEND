using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class TaxSlab
    {
        [Key]
        public int IdTaxSlab { get; set; }
        public int IdTaxConfig { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
    }
}
