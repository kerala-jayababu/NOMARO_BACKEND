namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ChildTaxThresholdDto
    {
        public int IdChildTaxThreshold { get; set; }
        public int IdTaxConfig { get; set; }
        public int ChildrenCount { get; set; }
        public decimal TaxThresholdAmount { get; set; }
    }
}
