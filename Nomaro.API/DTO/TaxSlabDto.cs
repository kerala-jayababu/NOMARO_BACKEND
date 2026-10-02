namespace Nomaro.API.DTO
{
    public class TaxSlabDto
    {
        public int IdTaxSlab { get; set; }
        public int IdTaxYearConfig { get; set; }
        public string AgeCategory { get; set; }
        public decimal IncomeFrom { get; set; }
        public decimal? IncomeTo { get; set; }
        public decimal TaxRate { get; set; }
    }
}
