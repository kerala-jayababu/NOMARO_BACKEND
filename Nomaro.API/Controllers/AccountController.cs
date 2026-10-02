using Asp.Versioning;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{

    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IEmployeeServices _employeeservice;
        public AccountController(IAccountService accountService, IEmployeeServices employeeservice)
        {
            _accountService = accountService;
            _employeeservice = employeeservice;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto login)
        {
            var result = await _accountService.Login(login);

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound("User Not Found");
        }
        [HttpPost("ValidateLogin")]
        public async Task<IActionResult> ValidateLogin(string emailId)
        {
            try
            {
                var result = await _accountService.ValidateLogin(emailId);

                if (result != null)
                {
                    return Ok(ApiResponseDto<string>.CreateSuccess(result, "Login validated successfully."));
                }

                return Unauthorized(ApiResponseDto<string>.CreateFailure("User not found or invalid permissions."));
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("SetOTP")]
        public async Task<IActionResult> SetOTP(string emailID)
        {
            try
            {
                var otp = await _employeeservice.SetOTP(emailID);
                return Ok(ApiResponseDto<OTPDto>.CreateSuccess(otp, "OTP processed."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("ValidateOTP")]
        public async Task<IActionResult> ValidateOTP(string emailID, string otp)
        {
            try
            {
                var otpStatus = await _employeeservice.ValidateOTP(emailID, otp);

                if (otpStatus == null || otpStatus.IdEmployee == 0)
                {
                    return Ok(ApiResponseDto<OTPStatusDto>.CreateSuccess(null, "Cannot validate OTP. It may be invalid or expired."));
                }

                return Ok(ApiResponseDto<OTPStatusDto>.CreateSuccess(otpStatus, "OTP validated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [AllowAnonymous]
        [HttpPost("LoginWithPassword")]
        public async Task<IActionResult> LoginWithPassword([FromBody] PasswordLoginDto login)
        {
            try
            {
                var result = await _accountService.LoginWithPassword(login);
                return Ok(ApiResponseDto<OTPStatusDto>.CreateSuccess(result, "Login successful."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [Authorize]
        [HttpGet("GetPasswordStatus")]
        public async Task<IActionResult> GetPasswordStatus()
        {
            try
            {
                if (!int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idEmployee))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Invalid session. Please login again."));

                var result = await _accountService.GetPasswordStatus(idEmployee);
                return Ok(ApiResponseDto<PasswordStatusDto>.CreateSuccess(result, "Password status fetched."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idEmployee))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Invalid session. Please login again."));

                await _accountService.ChangePassword(idEmployee, dto);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Password changed successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>Forgot password: send an OTP with SetOTP first, then reset the password with it.</summary>
        [AllowAnonymous]
        [HttpPost("ResetPasswordWithOTP")]
        public async Task<IActionResult> ResetPasswordWithOTP([FromBody] ResetPasswordDto dto)
        {
            try
            {
                await _accountService.ResetPasswordWithOTP(dto);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Password has been reset. You can now login with your new password."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("DecryptToken")]
        public async Task<IActionResult> DecryptToken(string token)
        {
            var result = await _accountService.DecryptToken(token);
            if (result != null)
            {
                return Ok(result);
            }

            return NotFound("User Not Found");

        }

    }
}

