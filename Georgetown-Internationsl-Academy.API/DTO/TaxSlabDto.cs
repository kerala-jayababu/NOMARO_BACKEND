namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class TaxSlabDto
    {
        public int IdTaxSlab { get; set; }
        public int IdTaxConfig { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
    }
}
