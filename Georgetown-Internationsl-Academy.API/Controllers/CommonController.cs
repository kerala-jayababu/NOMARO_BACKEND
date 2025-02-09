using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class CommonController : ControllerBase
    {
        private readonly IOptionService _optionService;

        public CommonController(IOptionService optionService)
        {
            _optionService = optionService;
        }

        [HttpGet("GetAllOptions")]
        public async Task<IActionResult> GetAllOptions()
        {
            try
            {
                var options = await _optionService.GetAllOptions();

                if (options == null)
                {
                    return NotFound("No options found.");
                }

                return Ok(options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetAllSalaryMonths")]
        public async Task<IActionResult> GetAllSalaryMonths()
        {
            var result = await _optionService.GetAllSalaryMonths();
            return Ok(result);
        }


        [HttpGet("GetHolidayTypes")]
        public async Task<IActionResult> GetHolidayTypes()
        {
            var result = await _optionService.GetHolidayTypes();
            return Ok(result);
        }


        [HttpGet("GetEmployeeLatestSalaryStructure")]
        public async Task<IActionResult> GetEmployeeLatestSalaryStructure()
        {
            var result = await _optionService.GetEmployeeLatestSalaryStructure();
            return Ok(result);
        }



    }
}
