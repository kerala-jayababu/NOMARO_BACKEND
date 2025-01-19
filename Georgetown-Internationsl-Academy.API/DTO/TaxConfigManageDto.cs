namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class TaxConfigManageDto
    {
        public int IdTaxConfig { get; set; }
        public string FinancialYearDesc { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        
    }
}
