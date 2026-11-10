using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    // Maps to NOMARO_GLOBAL.dbo.NomaroInstanceConfig - one row per company/tenant.
    [Table("NomaroInstanceConfig", Schema = "dbo")]
    public class NomaroInstanceConfig
    {
        [Key]
        public int IdNormaroConfig { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? DBConnectionString { get; set; }
        public string? DateFormat { get; set; }
        public string? CurrencySymbol { get; set; }
        public string? CurrencyName { get; set; }
        public string? CurrencyCode { get; set; }
    }
}
