using Asp.Versioning;
using FluentValidation;
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
    public class EmployeeStatutoryDetailsController : ControllerBase
    {
        private readonly IEmployeeStatutoryDetailsService _employeeStatutoryDetailsService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IValidator<EmployeeStatutoryDetailsDto> _validator;

        public EmployeeStatutoryDetailsController(
            IEmployeeStatutoryDetailsService employeeStatutoryDetailsService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService,
            IValidator<EmployeeStatutoryDetailsDto> validator)
        {
            _employeeStatutoryDetailsService = employeeStatutoryDetailsService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _validator = validator;
        }

        [HttpGet("GetEmployeeStatutoryDetails")]
        public async Task<IActionResult> GetEmployeeStatutoryDetails(string? searchText)
        {
            try
            {
                var details = await _employeeStatutoryDetailsService.GetEmployeeStatutoryDetails(searchText);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeStatutoryDetailsDto>>
                        .CreateSuccess(Enumerable.Empty<EmployeeStatutoryDetailsDto>(), "No statutory details available."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeStatutoryDetailsDto>>
                    .CreateSuccess(details, "Statutory details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>Returns data = null (not an error) when the employee has no statutory details yet.</summary>
        [HttpGet("GetEmployeeStatutoryDetailsById")]
        public async Task<IActionResult> GetEmployeeStatutoryDetailsById(int idEmployee)
        {
            if (idEmployee <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Select an employee."));
            }

            try
            {
                var details = await _employeeStatutoryDetailsService.GetEmployeeStatutoryDetailsById(idEmployee);

                if (details == null)
                {
                    return Ok(ApiResponseDto<EmployeeStatutoryDetailsDto?>
                        .CreateSuccess(null, "Statutory details are not set up for this employee."));
                }

                return Ok(ApiResponseDto<EmployeeStatutoryDetailsDto>
                    .CreateSuccess(details, "Statutory details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>States, e.g. GetStates?hasPT=true&amp;isActive=true for the PT State dropdown.</summary>
        [HttpGet("GetStates")]
        public async Task<IActionResult> GetStates(bool? hasPT, bool? hasLWF, bool? isActive)
        {
            try
            {
                var states = await _employeeStatutoryDetailsService.GetStates(hasPT, hasLWF, isActive);

                if (states == null || !states.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<StateDto>>
                        .CreateSuccess(Enumerable.Empty<StateDto>(), "No states available."));
                }

                return Ok(ApiResponseDto<IEnumerable<StateDto>>
                    .CreateSuccess(states, "States retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>Adds the employee's statutory details, or updates them when they already exist.</summary>
        [HttpPost("AddOrUpdateEmployeeStatutoryDetails")]
        public async Task<IActionResult> AddOrUpdateEmployeeStatutoryDetails([FromBody] EmployeeStatutoryDetailsDto dto)
        {
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                if (!await _employeeStatutoryDetailsService.IsEmployeeExists(dto.IdEmployee))
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Employee not found."));
                }

                var isUpdate = await _employeeStatutoryDetailsService.IsStatutoryDetailsExists(dto.IdEmployee);
                var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
                var actionType = isUpdate ? "U" : "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _employeeStatutoryDetailsService.AddOrUpdateEmployeeStatutoryDetails(dto, int.Parse(IdEmployee));

                if (!isSuccess)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save statutory details."));
                }

                var message = isUpdate ? "Statutory details updated successfully." : "Statutory details added successfully.";
                return Ok(ApiResponseDto<string>.CreateSuccess(message, message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
