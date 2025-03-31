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
        [HttpPost("DecryptToken")]
        public async Task<IActionResult> DecryptToken(string token)
        {
            var result = await _accountService.DecryptToken(token);

            string message = @$"<p>Hi ,</p>
                                 <h3>Your OTP for Infomed Conference Registration</h3>
                                 <h1>{result}</h1>
                                 <p>This OTP will be valid for 5 Minutes</p>
                                 <p>If you didn't request this code, you can safely ignore this email. 
                                 Someone else might have typed your email address by mistake</p>
                                 <p>Thanks</p>
                                 <h3>Infomed Team</h3>
                               ";
            //bool IsEmailSent = await EmailService.SendMail("sandeep241798@gmail.com", message, "Verification Code");

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound("User Not Found");

        }

    }
}
