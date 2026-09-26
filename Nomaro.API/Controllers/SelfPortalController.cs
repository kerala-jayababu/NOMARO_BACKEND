using Asp.Versioning;
using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Nomaro.API.Controllers
{
    
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class SelfPortalController : ControllerBase
    {

        private readonly IEmployeeServices _employeeservice;       
        private readonly IOvertimeTransactionService _overtimeTransactionService;       
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public SelfPortalController(IEmployeeServices employeeservice, IConfiguration configuration, IRoleBasedScreenService roleBasedService,IOvertimeTransactionService overtimeTransactionService)
        {
            _employeeservice = employeeservice;           
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _overtimeTransactionService = overtimeTransactionService;
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


        [HttpGet("GetOvertimeTransactionsForSelfPortal")]
        public async Task<IActionResult> GetOvertimeTransactionsForSelfPortal(int EmployeeId,  DateTime? date)
        {
            try
            {
                if (EmployeeId <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("EmployeeId must be greater than 0"));
                }

                var transactions = await _overtimeTransactionService.GetOvertimeTransactionsForSelfPortal(EmployeeId, date);

                if (transactions == null || !transactions.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<OvertimeTransactionDto>>.CreateSuccess(Enumerable.Empty<OvertimeTransactionDto>(), "No overtime transactions found."));
                }
                foreach (var transaction in transactions)
                {
                    if (!string.IsNullOrEmpty(transaction.Attachment) && System.IO.File.Exists(transaction.Attachment))
                    {
                        transaction.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(transaction.Attachment);
                    }
                }

                return Ok(ApiResponseDto<IEnumerable<OvertimeTransactionDto>>.CreateSuccess(transactions, "Overtime transactions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }





    }
}

