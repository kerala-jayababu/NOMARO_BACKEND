using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Controllers
{
    /// <summary>
    /// Public company registration page (no login, no company code in the URL).
    /// Flow: GetCountries -> CheckCompanyCode -> SendOtp -> VerifyOtp -> Register.
    /// </summary>
    [ApiController]
    [ApiVersion(1)]
    [AllowAnonymous]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class CompanyOnboardingController : ControllerBase
    {
        private readonly ICompanyOnboardingService _service;
        private readonly ILogger<CompanyOnboardingController> _logger;

        public CompanyOnboardingController(ICompanyOnboardingService service, ILogger<CompanyOnboardingController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

        private IActionResult Fail(Exception ex)
        {
            if (ex is InvalidOperationException) return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            _logger.LogError(ex, "Company onboarding request failed");
            return StatusCode(500, ApiResponseDto<string>.CreateFailure("Something went wrong. Please try again."));
        }

        [HttpGet("GetCountries")]
        public async Task<IActionResult> GetCountries()
        {
            try { return Ok(ApiResponseDto<List<OnboardingCountryDto>>.CreateSuccess(await _service.GetCountries(), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("CheckCompanyCode")]
        public async Task<IActionResult> CheckCompanyCode(string? companyCode, string? countryCode, string? email)
        {
            try { return Ok(ApiResponseDto<OnboardingCodeCheckDto>.CreateSuccess(await _service.CheckCompanyCode(companyCode, countryCode, email), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] OnboardingSendOtpDto dto)
        {
            try
            {
                var r = await _service.SendOtp(dto?.Email, Ip);
                return Ok(ApiResponseDto<OnboardingOtpResultDto>.CreateSuccess(r, $"A 6-digit code was sent to {r.MaskedEmail}."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OnboardingVerifyOtpDto dto)
        {
            try { return Ok(ApiResponseDto<OnboardingVerifiedDto>.CreateSuccess(await _service.VerifyOtp(dto?.Email, dto?.Otp), "Email verified.")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("Register")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
        public async Task<IActionResult> Register([FromForm] OnboardingRegisterDto dto)
        {
            try
            {
                var r = await _service.Register(dto, Ip);
                return Ok(ApiResponseDto<OnboardingResultDto>.CreateSuccess(r, $"{r.CompanyName} is registered."));
            }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
