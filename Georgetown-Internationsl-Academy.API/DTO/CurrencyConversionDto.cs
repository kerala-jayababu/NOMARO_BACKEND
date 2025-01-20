namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class CurrencyConversionDto
    {
        public int IdCurrencyConversion { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public DateTime RateDate { get; set; }
        public decimal ConversionRate { get; set; }

    }
}
