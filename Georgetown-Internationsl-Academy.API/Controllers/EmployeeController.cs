using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
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
        public EmployeeController(IEmployeeServices employeeservice)
        {
            _employeeservice = employeeservice;
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



    }
}
