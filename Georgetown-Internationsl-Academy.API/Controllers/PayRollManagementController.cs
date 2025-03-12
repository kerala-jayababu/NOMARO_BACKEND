using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Validators.PayrollManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class PayRollManagementController : ControllerBase
    {
        private readonly ICurrencyConversionService _currencyConversionService;
        private readonly IOvertimeTransactionService _overtimeTransactionService;
        private readonly IValidator<CurrencyConversionDto> _currencyConversionValidator;
        private readonly IValidator<OvertimeTransactionDto> _overtimeTransactionValidator;
        private readonly ISalaryAdjustmentService _salaryAdjustmentService;
        private readonly IValidator<SalaryAdjustmentDto> _salaryAdjustmentvalidator;
        private readonly IScheduledSalaryDeductionService _ScheduledSalaryDeductionScheduledSalaryDeductionservice;
        private readonly IValidator<ScheduledSalaryDeductionDto> _scheduledSalaryDeductionscheduledSalaryDeductionValidator;
        private readonly IMaternityLeaveSalaryService _maternityLeaveSalaryService;
        private readonly IValidator<MaternityLeaveSalaryDto> _maternityLeaveSalaryValidator;
        private readonly IRentFreeQuarterService _rentfreeservice;
        private readonly IValidator<RentFreeQuarterDto> _rentfreevalidator;
        private readonly IApprovalWorkflowService _approvalWorkflowService;
        public PayRollManagementController(ICurrencyConversionService currencyConversionService, 
            IValidator<CurrencyConversionDto> currencyConversionValidator,
            IOvertimeTransactionService overtimeTransactionService, 
            IValidator<OvertimeTransactionDto> overtimeTransactionValidator,
            ISalaryAdjustmentService salaryAdjustmentService, IValidator<SalaryAdjustmentDto> salaryAdjustmentvalidator,
            IScheduledSalaryDeductionService ScheduledSalaryDeductionservice,
            IValidator<ScheduledSalaryDeductionDto> scheduledSalaryDeductionValidator,
            IMaternityLeaveSalaryService maternityLeaveSalaryService, IValidator<MaternityLeaveSalaryDto> maternityLeaveSalaryValidator,
            IApprovalWorkflowService approvalWorkflowService,
            IRentFreeQuarterService rentfreeservice, IValidator<RentFreeQuarterDto> rentfreevalidator)
        {
            _currencyConversionService = currencyConversionService;
            _currencyConversionValidator = currencyConversionValidator;
            _overtimeTransactionService = overtimeTransactionService;
            _overtimeTransactionValidator = overtimeTransactionValidator;
            _salaryAdjustmentService = salaryAdjustmentService;
            _salaryAdjustmentvalidator = salaryAdjustmentvalidator;
            _ScheduledSalaryDeductionScheduledSalaryDeductionservice = ScheduledSalaryDeductionservice;
            _scheduledSalaryDeductionscheduledSalaryDeductionValidator = scheduledSalaryDeductionValidator;
            _maternityLeaveSalaryService = maternityLeaveSalaryService;
            _maternityLeaveSalaryValidator = maternityLeaveSalaryValidator;
            _approvalWorkflowService = approvalWorkflowService;
            _rentfreeservice = rentfreeservice;
            _rentfreevalidator = rentfreevalidator;
        }

        #region CurrencyConversions

        [HttpGet("GetAllCurrencyConversions")]
        public async Task<IActionResult> GetAllCurrencyConversions()
        {
            try
            {
                var conversions = await _currencyConversionService.GetAllCurrencyConversions();

                if (!conversions.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No currency conversions found."));

                return Ok(ApiResponseDto<IEnumerable<CurrencyConversionDto>>.CreateSuccess(conversions, "Currency conversions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetCurrencyConversionById")]
        public async Task<IActionResult> GetCurrencyConversionById(int id)
        {
            try
            {
                var conversion = await _currencyConversionService.GetCurrencyConversionById(id);

                if (conversion == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Currency conversion not found."));

                return Ok(ApiResponseDto<CurrencyConversionDto>.CreateSuccess(conversion, "Currency conversion retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddCurrencyConversion")]
        public async Task<IActionResult> AddCurrencyConversion([FromBody] CurrencyConversionDto dto)
        {
            var validationResult = await _currencyConversionValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _currencyConversionService.AddCurrencyConversion(dto);

                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create currency conversion."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Currency conversion created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateCurrencyConversion")]
        public async Task<IActionResult> UpdateCurrencyConversion([FromBody] CurrencyConversionDto dto)
        {
            var validationResult = await _currencyConversionValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdCurrencyConversion <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0."));

            try
            {
                var result = await _currencyConversionService.UpdateCurrencyConversion(dto);

                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update currency conversion."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Currency conversion updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

        #region OvertimeTransactions

        [HttpGet("GetOvertimeTransactions")]
        public async Task<IActionResult> GetOvertimeTransactions(int EmployeeId, string? searchText, DateTime? startDate, string? dropdownFilter = null)
        {
            try
            {
                if (EmployeeId <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("EmployeeId must be greater than 0"));
                }

                var transactions = await _overtimeTransactionService.GetOvertimeTransactionList(EmployeeId,searchText, startDate, dropdownFilter);

                if (!transactions.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No overtime transactions found."));
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

        [HttpGet("GetOvertimeTransactionById")]
        public async Task<IActionResult> GetOvertimeTransactionById(int id)
        {
            try
            {
                var transaction = await _overtimeTransactionService.GetOvertimeTransactionById(id);

                if (transaction == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Overtime transaction not found."));
                }
              
                    if (!string.IsNullOrEmpty(transaction.Attachment) && System.IO.File.Exists(transaction.Attachment))
                    {
                        transaction.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(transaction.Attachment);
                    }
                

                return Ok(ApiResponseDto<OvertimeTransactionDto>.CreateSuccess(transaction, "Overtime transaction retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOvertimeTransaction")]
        public async Task<IActionResult> AddOvertimeTransaction([FromForm] OvertimeTransactionDto dto)
        {
            var validationResult = await _overtimeTransactionValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _overtimeTransactionService.AddOvertimeTransaction(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add overtime transaction."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Overtime transaction added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateOvertimeTransaction")]
        public async Task<IActionResult> UpdateOvertimeTransaction([FromForm] OvertimeTransactionDto dto)
        {
            var validationResult = await _overtimeTransactionValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _overtimeTransactionService.UpdateOvertimeTransaction(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update overtime transaction."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Overtime transaction updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

        #region salaryAdjustments
        [HttpGet("GetAllSalaryAdjustments")]
        public async Task<IActionResult> GetAllSalaryAdjustments(string? searchText = null, DateTime? fromDate = null)
        {
            try
            {
                var adjustments = await _salaryAdjustmentService.GetAllSalaryAdjustments(searchText, fromDate);
                if (!adjustments.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salary adjustments found."));
                }
                return Ok(ApiResponseDto<IEnumerable<SalaryAdjustmentDto>>.CreateSuccess(adjustments, "Salary adjustments retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetSalaryAdjustmentById")]
        public async Task<IActionResult> GetSalaryAdjustmentById(int id)
        {
            try
            {
                var adjustment = await _salaryAdjustmentService.GetSalaryAdjustmentById(id);
                if (adjustment == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary adjustment not found."));
                }
                return Ok(ApiResponseDto<SalaryAdjustmentDto>.CreateSuccess(adjustment, "Salary adjustment retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddSalaryAdjustment")]
        public async Task<IActionResult> AddSalaryAdjustment([FromBody] SalaryAdjustmentDto dto)
        {
            var validationResult = await _salaryAdjustmentvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var result = await _salaryAdjustmentService.AddSalaryAdjustment(dto);
            if (result == null)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add salary adjustment."));
            }
            return Ok(ApiResponseDto<string>.CreateSuccess("Salary adjustment added successfully."));
        }

        [HttpPost("UpdateSalaryAdjustment")]
        public async Task<IActionResult> UpdateSalaryAdjustment([FromBody] SalaryAdjustmentDto dto)
        {
            var validationResult = await _salaryAdjustmentvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var result = await _salaryAdjustmentService.UpdateSalaryAdjustment(dto);
            if (result == null)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure("Salary adjustment not found."));
            }
            return Ok(ApiResponseDto<string>.CreateSuccess("Salary adjustment updated successfully."));
        }
        #endregion

        #region   ScheduledDeductions
        
     [HttpGet("GetAllScheduledSalaryDeductionservice")]
    public async Task<IActionResult> GetAllScheduledSalaryDeductionservice(string? searchText = null, DateTime? fromDate = null)
        {
            try
            {
                var deductions = await _ScheduledSalaryDeductionScheduledSalaryDeductionservice.GetScheduledDeductions(searchText,fromDate);
                return Ok(ApiResponseDto<IEnumerable<ScheduledSalaryDeductionDto>>.CreateSuccess(deductions, "Scheduled deductions retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetScheduledSalaryDeductionserviceById")]
        public async Task<IActionResult> GetScheduledSalaryDeductionserviceById(int id)
        {
            try
            {
                var deduction = await _ScheduledSalaryDeductionScheduledSalaryDeductionservice.GetScheduledDeductionById(id);
                if (deduction == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Scheduled deduction not found."));

                return Ok(ApiResponseDto<ScheduledSalaryDeductionDto>.CreateSuccess(deduction, "Scheduled deduction retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddscheduledSalaryDeductionservice")]
        public async Task<IActionResult> AddscheduledSalaryDeductionservice([FromBody] ScheduledSalaryDeductionDto dto)
        {
            var validationResult = await _scheduledSalaryDeductionscheduledSalaryDeductionValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _ScheduledSalaryDeductionScheduledSalaryDeductionservice.AddScheduledDeduction(dto, int.Parse(IdEmployee));
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create scheduled deduction."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Scheduled deduction created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdatescheduledSalaryDeductionservice")]
        public async Task<IActionResult> UpdatescheduledSalaryDeductionservice(ScheduledSalaryDeductionDto dto)
        {
            var validationResult = await _scheduledSalaryDeductionscheduledSalaryDeductionValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _ScheduledSalaryDeductionScheduledSalaryDeductionservice.UpdateScheduledDeduction(dto, int.Parse(IdEmployee));
                if (result == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Scheduled deduction not found."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Scheduled deduction updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

        #region MaternityLeaveSalaries

        [HttpGet("GetAllMaternityLeaveSalaries")]
        public async Task<IActionResult> GetAllMaternityLeaveSalaries(string? searchText = null, DateTime? fromDate = null)
        {
            try
            {
                var leaveSalaries = await _maternityLeaveSalaryService.GetAllMaternityLeaveSalaries(searchText,fromDate);

                if (!leaveSalaries.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No maternity leave salaries found."));
                }

                return Ok(ApiResponseDto<IEnumerable<MaternityLeaveSalaryDto>>.CreateSuccess(leaveSalaries, "Maternity leave salaries retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetMaternityLeaveSalaryById")]
        public async Task<IActionResult> GetMaternityLeaveSalaryById(int id)
        {
            try
            {
                var leaveSalary = await _maternityLeaveSalaryService.GetMaternityLeaveSalaryById(id);

                if (leaveSalary == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Maternity leave salary not found."));
                }

                return Ok(ApiResponseDto<MaternityLeaveSalaryDto>.CreateSuccess(leaveSalary, "Maternity leave salary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddMaternityLeaveSalary")]
        public async Task<IActionResult> AddMaternityLeaveSalary([FromBody] MaternityLeaveSalaryDto dto)
        {
            var validationResult = await _maternityLeaveSalaryValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var result = await _employeeSalaryConfigService.AddConfig(dto, int.Parse(IdEmployee));
                var result = await _maternityLeaveSalaryService.AddMaternityLeaveSalary(dto, int.Parse(IdEmployee));

                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create maternity leave salary."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateMaternityLeaveSalary")]
        public async Task<IActionResult> UpdateMaternityLeaveSalary([FromBody] MaternityLeaveSalaryDto dto)
        {
            var validationResult = await _maternityLeaveSalaryValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdMaternityLeaveSalary <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid ID. ID must be greater than 0."));
            }

            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _maternityLeaveSalaryService.UpdateMaternityLeaveSalary(dto, int.Parse(IdEmployee));

                if (result == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Maternity leave salary not found."));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region rentFreeQuarter
        [HttpGet("GetRentFreeQuarterList")]
        public async Task<IActionResult> GetRentFreeQuarterList()
        {
            try
            {
                var list = await _rentfreeservice.GetRentFreeQuarters();
                if (!list.Any())
                    return NotFound(ApiResponseDto<string>.CreateFailure("No Rent-Free Quarters found."));

                return Ok(ApiResponseDto<IEnumerable<RentFreeQuarterDto>>.CreateSuccess(list, "Rent-Free Quarters retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetRentFreeQuarterByID")]
        public async Task<IActionResult> GetRentFreeQuarterByID(int id)
        {
            try
            {
                var rentFreeQuarter = await _rentfreeservice.GetRentFreeQuarterById(id);
                if (rentFreeQuarter == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("No Rent-Free Quarter found."));

                return Ok(ApiResponseDto<RentFreeQuarterDto>.CreateSuccess(rentFreeQuarter, "Rent-Free Quarter retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddRentFreeQuarter")]
        public async Task<IActionResult> AddRentFreeQuarter([FromBody] RentFreeQuarterDto dto)
        {
            var validationResult = await _rentfreevalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}"));

            try
            {
                var result = await _rentfreeservice.AddRentFreeQuarter(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add Rent-Free Quarter."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Rent-Free Quarter added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateRentFreeQuarter")]
        public async Task<IActionResult> UpdateRentFreeQuarter([FromBody] RentFreeQuarterDto dto)
        {
            var validationResult = await _rentfreevalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}"));

            try
            {
                var result = await _rentfreeservice.UpdateRentFreeQuarter(dto);
                if (result == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Rent-Free Quarter not found."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Rent-Free Quarter updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region ConfigApprovals
        [HttpGet("GetConfigApprovalsList")]
        public async Task<IActionResult> GetConfigApprovalsList([FromQuery] DateTime fromDate,[FromQuery] string? actionStatus = null, [FromQuery] string? entityCode = null )
        {
            try
            {
                if (fromDate == default(DateTime)) // Check if fromDate has an invalid default value
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("The 'fromDate' parameter is required and must be a valid date."));
                }
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(IdEmployee))
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid employee ID."));
                }

                var workflows = await _approvalWorkflowService.GetConfigApprovalsList(fromDate,actionStatus, entityCode, IdEmployee);
                if (!workflows.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No ConfigApprovalsList records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ConfigApprovalsDto>>.CreateSuccess(workflows, "Salary list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion
    }
}
