using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class RoleBasedScreensController : ControllerBase
    {
        private readonly IRoleBasedScreenService _roleBasedScreenService;
        private readonly IValidator<PayrollScreenDto> _payRollScreenValidator;
        private readonly IValidator<EmployeePermissionDto> _employeePermissionValidator;

        public RoleBasedScreensController(IRoleBasedScreenService roleBasedScreenService,
            IValidator<PayrollScreenDto> payRollScreenValidator,
            IValidator<EmployeePermissionDto> employeePermissionValidator)
        {
            _roleBasedScreenService = roleBasedScreenService;
            _payRollScreenValidator = payRollScreenValidator;
            _employeePermissionValidator = employeePermissionValidator;
        }

        [HttpGet("GetAllPayrollScreens")]
        public async Task<IActionResult> GetAllPayrollScreens()
        {
            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var payrollScreens = await _roleBasedScreenService.GetAllPayrollScreens(int.Parse(IdEmployee));

                if (payrollScreens == null || !payrollScreens.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No payroll screens found."));
                }

                return Ok(ApiResponseDto<IEnumerable<PayrollScreenDto>>.CreateSuccess(payrollScreens, "Payroll screens retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManagePayrollScreens")]
        public async Task<IActionResult> ManagePayrollScreens([FromBody] List<PayrollScreenDto> payrollScreens)
        {
            if (payrollScreens == null || !payrollScreens.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Payroll screens list cannot be null or empty."));
            }

            try
            {
                var result = await _roleBasedScreenService.ManagePayrollScreens(payrollScreens);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage payroll screens."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Payroll screens managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeePermissionsById")]
        public async Task<IActionResult> GetEmployeePermissionsById(int id)
        {
            try
            {
                var permissions = await _roleBasedScreenService.GetEmployeePermissionsById(id);

                if (permissions == null || !permissions.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No employee permissions found."));
                }

                return Ok(ApiResponseDto<IEnumerable<PayrollScreenDto>>.CreateSuccess(permissions, "Employee permissions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageEmployeePermissions")]
        public async Task<IActionResult> ManageEmployeePermissions([FromBody] List<EmployeePermissionDto> employeePermissions)
        {
            if (employeePermissions == null || !employeePermissions.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee permissions list cannot be null or empty."));
            }

            try
            {
                var result = await _roleBasedScreenService.ManageEmployeePermissions(employeePermissions);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage employee permissions."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee permissions managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetRoleBasedPermissionsByDesignationId")]
        public async Task<IActionResult> GetRoleBasedPermissionsByDesignationId(int designationId)
        {
            if (designationId <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Designation ID."));
            }

            try
            {
                var permissions = await _roleBasedScreenService.GetRoleBasedPermissionsByDesignationId(designationId);

                if (permissions == null || !permissions.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure($"No permissions found for Designation ID: {designationId}."));
                }

                return Ok(ApiResponseDto<IEnumerable<RoleBasedScreenDto>>.CreateSuccess(permissions, "Role-based permissions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageRoleBasedPermissions")]
        public async Task<IActionResult> ManageRoleBasedPermissions([FromBody] List<RoleBasedPermissionDto> rolePermissions)
        {
            if (rolePermissions == null || !rolePermissions.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Role-based permissions list cannot be null or empty."));
            }

            try
            {
                var result = await _roleBasedScreenService.ManageRoleBasedPermissions(rolePermissions);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage role-based permissions."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Role-based permissions managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
