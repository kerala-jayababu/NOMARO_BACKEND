using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class SalaryGenerationController : ControllerBase
    {
        private readonly ISalaryGenerationService _salaryService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;


        public SalaryGenerationController(ISalaryGenerationService salaryService, IConfiguration configuration, IRoleBasedScreenService roleBasedService)
        {
            _salaryService = salaryService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;

        }

        /// <summary>
        /// Gets the salary list based on filters.
        /// </summary>
        /// <param name="idSalaryMonth">Salary month ID (Optional)</param>
        /// <param name="searchText">Search text for Employee Code, Name, Department, Designation (Optional)</param>
        /// <param name="dropdownFilter">Status filter: All, Submitted, Approved, Draft Generated, Not Generated (Optional)</param>
        /// <param name="idDepartment">Filter by Department ID (Optional)</param>
        /// <param name="idDesignation">Filter by Designation ID (Optional)</param>
        /// <returns>List of salary configurations</returns>
        [HttpGet("GetSalaryList")]
        public async Task<IActionResult> GetSalaryList(
            [FromQuery] int? idSalaryMonth = null,          
            [FromQuery] string? dropdownFilter = null,
            [FromQuery] int? idDepartment = null,
            [FromQuery] int? idDesignation = null)
        {
            try
            {
                if (idSalaryMonth == null || idSalaryMonth <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary genetation list."));
                }

                IEnumerable<SalaryGenerationDto>? salaryList = await _salaryService.GetSalaryConfigs(idSalaryMonth, dropdownFilter, idDepartment, idDesignation);

                if (salaryList == null || !salaryList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDto>>.CreateSuccess(Enumerable.Empty<SalaryGenerationDto>(), "No salary records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDto>>.CreateSuccess(salaryList, "Salary list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("GenerateSalaryDraft")]
        public async Task<IActionResult> GenerateSalaryDraft([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth == null || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary genetation list."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idEmployee))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("User not authenticated."));
                }

                var result = await _salaryService.GenerateSalaryDraft(employeeIds, idSalaryMonth, int.Parse(idEmployee));

                if (!result.Any())
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to generate draft salary."));
                }

                return Ok(ApiResponseDto<IEnumerable<dynamic>>.CreateSuccess(result, "Draft Salary generated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("SubmitSalaryDetails")]
        public async Task<IActionResult> SubmitSalaryDetails([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth == null || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for submitting salary."));
            }
            try
            {
                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idEmployee))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("User not authenticated."));
                }

                var result = await _salaryService.SubmitSalaryDetails(employeeIds, idSalaryMonth, int.Parse(idEmployee));

                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to submit salary."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary Submitted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UndoGeneratedDraftSalary")]
        public async Task<IActionResult> UndoGeneratedDraftSalary([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "D";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var deletedCount = await _salaryService.UndoGeneratedDraftSalary(employeeIds, idSalaryMonth);

                if (deletedCount == 0)
                {
                    return Ok(ApiResponseDto<string>.CreateSuccess("No records found to delete."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess($"Successfully deleted {deletedCount} draft salary records."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }




        [HttpGet("GetSalaryGeneratedDetails")]
        public async Task<IActionResult> GetSalaryGeneratedDetails([FromQuery] int? idSalaryMonth = null, [FromQuery] string? dropdownFilter = null)
        {
            if (!idSalaryMonth.HasValue || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for retrieving the salary generation details list."));
            }

            try
            {
                var salaryList = await _salaryService.GetSalaryGeneratedDetails(idSalaryMonth, dropdownFilter);

                if (salaryList == null || !salaryList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDetailsDto>>.CreateSuccess(Enumerable.Empty<SalaryGenerationDetailsDto>(), "No salary records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDetailsDto>>.CreateSuccess(salaryList, "Salary generation details list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


    }
}
