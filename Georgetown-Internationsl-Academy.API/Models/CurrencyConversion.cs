using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class CurrencyConversion
    {
        [Key]
        public int IdCurrencyConversion { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public DateTime RateDate { get; set; }
        [Column(TypeName = "numeric(14,5)")]
        public decimal ConversionRate { get; set; }
    }
}
