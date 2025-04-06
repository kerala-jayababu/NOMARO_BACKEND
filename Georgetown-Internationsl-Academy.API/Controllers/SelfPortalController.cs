using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class SelfPortalController : ControllerBase
    {

        private readonly IEmployeeServices _employeeservice;       
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public SelfPortalController(IEmployeeServices employeeservice, IConfiguration configuration, IRoleBasedScreenService roleBasedService)
        {
            _employeeservice = employeeservice;           
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        [HttpGet("GetPaySlipDetails")]
        public async Task<IActionResult> GetPaySlipDetails(int IdEmployee, int 	IdSalaryMonth)
        {
            try
            {               

                if (IdEmployee == null || IdSalaryMonth == null )
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid IdEmployee or Invalid IdSalaryMonth"));
                }

                var payslipdetails = await _employeeservice.GetPayslipDetails(IdEmployee, IdSalaryMonth);
                if (payslipdetails == null || !payslipdetails.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<PayslipDetailsDto>>.CreateSuccess(Enumerable.Empty<PayslipDetailsDto>(), "No PaySlipDetails found for the employeee."));
                }
                return Ok(ApiResponseDto<IEnumerable<PayslipDetailsDto>>.CreateSuccess(payslipdetails, "PaySlipDetails details retrieved successfully."));
            }
            catch (Exception ex)
            {

                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetSalaryDetailsEmployee")]
        public async Task<IActionResult> GetSalaryDetailsEmployee(int IdEmployee, int IdSalaryMonthFrom,int IdSalaryMonthTo)
        {
            try
            {

                if (IdEmployee == null || IdSalaryMonthFrom == null||IdSalaryMonthTo == null)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid IdEmployee or Invalid IdSalaryMonthFrom or Invalid IdSalaryMonthTo"));
                }

                var payslipdetails = await _employeeservice.GetSalaryDetailsEmployee(IdEmployee, IdSalaryMonthFrom, IdSalaryMonthTo);
                if (payslipdetails == null || !payslipdetails.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryDetailsEmployeeDto>>.CreateSuccess(Enumerable.Empty<SalaryDetailsEmployeeDto>(), "No SalaryDetails found for the employeee."));
                }
                return Ok(ApiResponseDto<IEnumerable<SalaryDetailsEmployeeDto>>.CreateSuccess(payslipdetails, "SalaryDetails details retrieved successfully."));
            }
            catch (Exception ex)
            {

                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }





    }
}
