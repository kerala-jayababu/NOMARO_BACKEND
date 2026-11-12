namespace Nomaro.API.DTO
{
    public class OnboardingCountryDto
    {
        public string CountryCode { get; set; } = "";
        public string CountryName { get; set; } = "";
    }

    public class OnboardingCodeCheckDto
    {
        public string CompanyCode { get; set; } = "";
        public bool Available { get; set; }
        public string? Message { get; set; }
    }

    public class OnboardingSendOtpDto
    {
        public string? Email { get; set; }
    }

    public class OnboardingVerifyOtpDto
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
    }

    public class OnboardingOtpResultDto
    {
        public int ValidSeconds { get; set; }
        public string? MaskedEmail { get; set; }
    }

    public class OnboardingVerifiedDto
    {
        public string Token { get; set; } = "";
        public int ValidMinutes { get; set; }
    }

    public class OnboardingRegisterDto
    {
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? CountryCode { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? ContactName { get; set; }
        public string? ContactMobile { get; set; }
        public IFormFile? Logo { get; set; }
    }

    public class OnboardingResultDto
    {
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string CompanyUrl { get; set; } = "";
        public string AdminEmail { get; set; } = "";
        public bool EmailSent { get; set; }
    }

    /// <summary>Raised when a step fails after the registration row exists; the user can retry the same submission.</summary>
    public class OnboardingStepException : Exception
    {
        public string Step { get; }
        public OnboardingStepException(string step, string message, Exception? inner = null) : base(message, inner) { Step = step; }
    }
}
