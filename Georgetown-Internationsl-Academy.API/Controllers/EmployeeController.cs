using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
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

    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeServices _employeeservice;
        private readonly IValidator<List<EmployeeBankAccountDtoList>> _employeeBankAccountvalidator;
        private readonly IValidator<List<EmployeeOvertimeConfigDtoList>> _employeeOverTimevalidator;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public EmployeeController(IEmployeeServices employeeservice, IValidator<List<EmployeeBankAccountDtoList>> employeeBankAccountvalidator,
    IConfiguration configuration, IRoleBasedScreenService roleBasedService, IValidator<List<EmployeeOvertimeConfigDtoList>> employeeOverTimevalidator)
        {
            _employeeservice = employeeservice;
            _employeeBankAccountvalidator = employeeBankAccountvalidator;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _employeeOverTimevalidator = employeeOverTimevalidator;
        }


        [HttpGet("GetEmployeeList")]
        public async Task<IActionResult> GetEmployeeList(string? searchText, DateTime? startDate)
        {
            try
            {
                var employeeList = await _employeeservice.GetEmployeeList(searchText, startDate);

                if (employeeList == null || !employeeList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDto>>.CreateSuccess(Enumerable.Empty<EmployeeProfileDto>(), "No employees found."));
                }

                foreach (var employee in employeeList)
                {
                    if (!string.IsNullOrEmpty(employee.EmployeePhotoFilePath) && System.IO.File.Exists(employee.EmployeePhotoFilePath))
                    {
                        employee.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(employee.EmployeePhotoFilePath);
                    }
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDto>>.CreateSuccess(employeeList, "Employee list retrieved successfully."));
            }
            catch (Exception ex)
            {
              
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeeDetailsByID")]
        public async Task<IActionResult> GetEmployeeDetailsByID(int Id)
        {
            try
            {
                var employeeDetails = await _employeeservice.GetEmployeeDetailsByID(Id);

              
                if (employeeDetails == null)
                {
                    return Ok(ApiResponseDto<EmployeeDetailsDto>.CreateSuccess(null, "No employee found with the provided ID."));
                }

               
                 if (!string.IsNullOrEmpty(employeeDetails.EmployeePhotoFilePath) && System.IO.File.Exists(employeeDetails.EmployeePhotoFilePath))
                    {
                    employeeDetails.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(employeeDetails.EmployeePhotoFilePath);
                   }
               
                return Ok(ApiResponseDto<EmployeeDetailsDto>.CreateSuccess(employeeDetails, "Employee Details  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeeProfileByID")]
        public async Task<IActionResult> GetEmployeeProfileByID(int Id)
        {
            try
            {
                var employeeProfile = await _employeeservice.GetEmployeeProfileByID(Id);

                if (employeeProfile == null || !employeeProfile.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDetailsDto>>.CreateSuccess(Enumerable.Empty<EmployeeProfileDetailsDto>(), "No employee bank account details found."));
                }

                foreach (var employee in employeeProfile)
                {
                    if (!string.IsNullOrEmpty(employee.EmployeePhotoFilePath) && System.IO.File.Exists(employee.EmployeePhotoFilePath))
                    {
                        employee.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(employee.EmployeePhotoFilePath);
                    }
                }
                return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDetailsDto>>.CreateSuccess(employeeProfile, "Employee profile details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeBankAccountsByID")]
        public async Task<IActionResult> GetEmployeeBankAccountsByID(int Id)
        {
            try
            {
                var employeeBankAccounts = await _employeeservice.GetEmployeeBankAccountsByID(Id);

                if (employeeBankAccounts == null || !employeeBankAccounts.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeBankAccountDto>>.CreateSuccess(Enumerable.Empty<EmployeeBankAccountDto>(), "No employee bank account details found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeBankAccountDto>>.CreateSuccess(employeeBankAccounts, "Employee bank account details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeOvertimeConfigsByID")]
        public async Task<IActionResult> GetEmployeeOvertimeConfigsByID(int employeeId)
        {
            try
            {
                var overtimeConfigs = await _employeeservice.GetEmployeeOvertimeConfigsByID(employeeId);

                if (overtimeConfigs == null || !overtimeConfigs.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeOvertimeConfigDto>>.CreateSuccess(Enumerable.Empty<EmployeeOvertimeConfigDto>(), "No overtime configurations found for the employee."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeOvertimeConfigDto>>.CreateSuccess(overtimeConfigs, "Overtime configurations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
     


        [HttpPost("UpdateEmployeeDetails")]

        public async Task<IActionResult> UpdateEmployeeDetails([FromBody]  UpdateEmployeeDto dto)
        {
            if (dto.EmployeeId <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Employee ID. Employee ID must be greater than 0."));
            }
            if (dto.BudgetCodeId <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Budget Code ID. Budget Code ID must be greater than 0."));
            }
            if (dto.ChildCount < 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Child Count. Child Count cannot be negative."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }


            
            try
            {                
                var updateResult = await _employeeservice.UpdateEmployeeDetails(dto);
                if (!updateResult)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update employee."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
            
        }


        [HttpPost("ManageEmployeeBankAccounts")]
        public async Task<IActionResult> ManageEmployeeBankAccounts([FromBody] List<EmployeeBankAccountDtoList> bankAccounts)
        {
            if (bankAccounts == null || !bankAccounts.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Bank account list cannot be empty."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var result = await _employeeservice.ManageEmployeeBankAccounts(bankAccounts);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage employee bank accounts."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee bank accounts managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageEmployeeOvertimeConfigs")]
        public async Task<IActionResult> ManageEmployeeOvertimeConfigs([FromBody] List<EmployeeOvertimeConfigDtoList> overtimeConfigs)
        {
            if (overtimeConfigs == null || !overtimeConfigs.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Overtime config list cannot be null or empty."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }

            try
            {
                var result = await _employeeservice.ManageEmployeeOvertimeConfigs(overtimeConfigs);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage employee overtime configs."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee overtime configs managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeesByHierarchy")]
        public async Task<IActionResult> GetEmployeesByHierarchy(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Employee ID."));
                }

                var result = await _employeeservice.GetEmployeesByHierarchy(employeeId);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeHierarchyDto>>.CreateSuccess(Enumerable.Empty<EmployeeHierarchyDto>(), "No employees found for the given hierarchy."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeHierarchyDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
