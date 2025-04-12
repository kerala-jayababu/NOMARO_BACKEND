using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{

    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

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
