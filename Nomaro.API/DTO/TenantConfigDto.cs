namespace Nomaro.API.DTO
{
    // Public company/tenant configuration returned to the frontend.
    // DBConnectionString is intentionally never exposed; only the database name is shared.
    public class TenantConfigDto
    {
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? DatabaseName { get; set; }
        public string? DateFormat { get; set; }
        public string? CurrencySymbol { get; set; }
        public string? CurrencyName { get; set; }
        public string? CurrencyCode { get; set; }
    }
}
