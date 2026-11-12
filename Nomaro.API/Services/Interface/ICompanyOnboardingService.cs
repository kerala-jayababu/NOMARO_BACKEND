using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    /// <summary>Public company self-registration: OTP, database creation from the country template, NOMARO_GLOBAL registration.</summary>
    public interface ICompanyOnboardingService
    {
        Task<List<OnboardingCountryDto>> GetCountries();
        Task<OnboardingCodeCheckDto> CheckCompanyCode(string? companyCode, string? countryCode, string? email);
        Task<OnboardingOtpResultDto> SendOtp(string? email, string? ipAddress);
        Task<OnboardingVerifiedDto> VerifyOtp(string? email, string? otp);
        Task<OnboardingResultDto> Register(OnboardingRegisterDto dto, string? ipAddress);
    }
}
