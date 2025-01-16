using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IOptionService _optionService;

        public CommonController(IOptionService optionService)
        {
            _optionService = optionService;
        }

        [HttpGet("GetSalaryHeadsOptions")]
        public async Task<IActionResult> GetSalaryHeadsOptions()
        {
            var options = await _optionService.GetSalaryHeadsOptions();
            return Ok(options);
        }

        [HttpGet("GetBudgetCodeOptions")]
        public async Task<IActionResult> GetBudgetCodeOptions()
        {
            var options = await _optionService.GetBudgetCodeOptions();
            return Ok(options);
        }
        [HttpGet("GetBanksOptions")]
        public async Task<IActionResult> GetBanksOptions()
        {
            var options = await _optionService.GetBanksOptions();
            return Ok(options);
        }
    }
}
