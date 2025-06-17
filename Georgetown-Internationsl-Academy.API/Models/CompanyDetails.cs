namespace Georgetown_Internationsl_Academy.API.Models
{
    public class CompanyDetails
    {
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? RegNumber { get; set; }
        public string TINNumber { get; set; }
        public string TaxAuthorizedPersonName { get; set; }
        public string TaxOfficeAddress { get; set; }
        public byte[]? SignatureImage { get; set; }
    }
}
