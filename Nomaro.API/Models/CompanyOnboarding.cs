using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    // NOMARO_GLOBAL.dbo.CompanyRegistrations - one row per self-registration attempt (see NOMARO_GLOBAL_CompanyOnboarding.sql).
    [Table("CompanyRegistrations", Schema = "dbo")]
    public class CompanyRegistration
    {
        [Key]
        public int IdCompanyRegistration { get; set; }
        public string CountryCode { get; set; } = "";
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? CompanyAddress { get; set; }
        public string Email { get; set; } = "";
        public string? ContactName { get; set; }
        public string? ContactMobile { get; set; }
        public byte[]? LogoContent { get; set; }
        public string? LogoMimeType { get; set; }
        public string? DatabaseName { get; set; }
        public string Status { get; set; } = "";
        public string? LastStep { get; set; }
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; }
        public string? CompanyUrl { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
    }

    // NOMARO_GLOBAL.dbo.CompanyRegistrationOtps - email verification codes (hashed).
    [Table("CompanyRegistrationOtps", Schema = "dbo")]
    public class CompanyRegistrationOtp
    {
        [Key]
        public int IdCompanyRegistrationOtp { get; set; }
        public string Email { get; set; } = "";
        public string OtpHash { get; set; } = "";
        public DateTime ExpiresOn { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public string? TokenHash { get; set; }
        public DateTime? TokenExpiresOn { get; set; }
        public DateTime? UsedOn { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
