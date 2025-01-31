using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeServices _employeeservice;
        private readonly IValidator<List<EmployeeBankAccountDtoList>> _employeeBankAccountvalidator;
        private readonly IValidator<List<EmployeeOvertimeConfigDtoList>> _employeeOverTimevalidator;
        public EmployeeController(IEmployeeServices employeeservice, IValidator<List<EmployeeBankAccountDtoList>> employeeBankAccountvalidator, IValidator<List<EmployeeOvertimeConfigDtoList>> employeeOverTimevalidator)
        {
            _employeeservice = employeeservice;
            _employeeBankAccountvalidator = employeeBankAccountvalidator;
            _employeeOverTimevalidator = employeeOverTimevalidator;
        }


        [HttpGet("GetEmployeeList")]
        public async Task<IActionResult> GetEmployeeList()
        {
            try
            {
                var employeeList = await _employeeservice.GetEmployeeList();

                if (!employeeList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No employees found."));
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
                    return NotFound(ApiResponseDto<string>.CreateFailure("Employee Details not found."));
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
                    return NotFound(ApiResponseDto<string>.CreateFailure("Employee bank account details not found."));
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
                    return NotFound(ApiResponseDto<string>.CreateFailure("Employee bank account details not found."));
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
                    return NotFound(ApiResponseDto<string>.CreateFailure("No overtime configurations found for the employee."));
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






    }
}
