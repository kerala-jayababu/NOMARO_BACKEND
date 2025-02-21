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
       

        public SalaryGenerationController(ISalaryGenerationService salaryService)
        {
            _salaryService = salaryService;
            
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

                if (!salaryList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salary records found."));
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

                return Ok(ApiResponseDto<string>.CreateSuccess( "Draft Salary generated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UndoGeneratedDraftSalary")]
        public async Task<IActionResult> UndoGeneratedDraftSalary([FromQuery]string  employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }

            try
            {
                var deletedCount = await _salaryService.UndoGeneratedDraftSalary(employeeIds, idSalaryMonth);

                if (deletedCount == 0)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No records found to delete."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess($"Successfully deleted {deletedCount} draft salary records."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


    }
}
