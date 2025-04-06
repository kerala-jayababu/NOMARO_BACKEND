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



       
            //bool IsEmailSent = await EmailService.SendMail("sandeep241798@gmail.com", message, "Verification Code");

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound("User Not Found");

        }

    }
}
