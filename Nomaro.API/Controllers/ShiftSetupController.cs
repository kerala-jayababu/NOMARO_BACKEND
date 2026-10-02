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
    public class ShiftSetupController : ControllerBase
    {
        private readonly IShiftSetupService _shiftSetupService;

        public ShiftSetupController(IShiftSetupService shiftSetupService)
        {
            _shiftSetupService = shiftSetupService;
        }

        [HttpGet("GetShiftSetupDetails")]
        public async Task<IActionResult> GetShiftSetupDetails()
        {
            var employeeIdClaim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var offices = await _shiftSetupService.GetShiftSetupDetails(employeeId);
                return Ok(ApiResponseDto<IEnumerable<ShiftSetupOfficeDto>>.CreateSuccess(
                    offices,
                    offices.Any() ? "Shift setup hierarchy retrieved successfully." : "No offices found for this employee hierarchy."));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetShiftSetupHierarchyByOffice")]
        public async Task<IActionResult> GetShiftSetupHierarchyByOffice(int idOffice)
        {
            if (idOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            var employeeIdClaim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var offices = await _shiftSetupService.GetShiftSetupHierarchyByOffice(employeeId, idOffice);
                return Ok(ApiResponseDto<IEnumerable<ShiftSetupOfficeDto>>.CreateSuccess(
                    offices,
                    "Office shift setup hierarchy retrieved successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetShiftManagerEmployees")]
        public async Task<IActionResult> GetShiftManagerEmployees(int idOffice)
        {
            if (idOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            var employeeIdClaim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var employees = await _shiftSetupService.GetShiftManagerEmployees(employeeId, idOffice);
                return Ok(ApiResponseDto<IEnumerable<ShiftManagerEmployeeDto>>.CreateSuccess(
                    employees,
                    "Active office employees retrieved successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateShiftManagerAssignments")]
        public async Task<IActionResult> UpdateShiftManagerAssignments(
            [FromBody] List<ShiftManagerAssignmentDto> assignments)
        {
            var employeeIdClaim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                await _shiftSetupService.UpdateShiftManagerAssignments(employeeId, assignments);
                return Ok(ApiResponseDto<string>.CreateSuccess("Shift managers updated successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}