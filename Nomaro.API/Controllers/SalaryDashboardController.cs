using Asp.Versioning;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class SalaryDashboardController : ControllerBase
    {
        private readonly ISalaryDashboardService _salaryDashboardService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public SalaryDashboardController(
            ISalaryDashboardService salaryDashboardService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService)
        {
            _salaryDashboardService = salaryDashboardService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        [HttpGet("GetFilterOptions")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var denied = await CheckViewPermission();
            if (denied != null) return denied;

            try
            {
                var options = await _salaryDashboardService.GetFilterOptions();
                return Ok(ApiResponseDto<SalaryDashboardFilterDto>.CreateSuccess(options, "Filter options retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <param name="idSalaryMonth">Salary month to show.</param>
        /// <param name="idOffice">Optional office filter.</param>
        /// <param name="idDepartment">Optional department filter.</param>
        /// <param name="approvedOnly">true = only APPROVED salaries (default); false = all generated salaries except rejected.</param>
        [HttpGet("GetSalaryDashboard")]
        public async Task<IActionResult> GetSalaryDashboard(int idSalaryMonth, int? idOffice, int? idDepartment, bool approvedOnly = true)
        {
            if (idSalaryMonth <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Select a salary month."));

            var denied = await CheckViewPermission();
            if (denied != null) return denied;

            try
            {
                var dashboard = await _salaryDashboardService.GetSalaryDashboard(idSalaryMonth, idOffice, idDepartment, approvedOnly);
                return Ok(ApiResponseDto<SalaryDashboardDto>.CreateSuccess(dashboard, "Salary dashboard retrieved successfully."));
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

        private async Task<IActionResult?> CheckViewPermission()
        {
            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            var screenCode = _configuration["ScreenCodes:SalaryDashboard"];
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(idEmployee), screenCode, "V");
            if (!hasPermission)
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission to view the Salary Dashboard."));

            return null;
        }
    }
}
