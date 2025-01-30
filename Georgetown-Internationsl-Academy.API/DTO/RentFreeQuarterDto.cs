namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class RentFreeQuarterDto
    {
        public int IdRentFreeQuater { get; set; }
        public int IdEmployee { get; set; }
        public decimal TotalAnnualRent { get; set; }
        public int DurationInMonths { get; set; }
        public DateTime ValidFrom { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal TaxRate { get; set; }
        public decimal AnnualTaxAmount { get; set; }
        public decimal MonthlyTaxAmount { get; set; }
    }
}
