using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeSalaryConfigController : ControllerBase
    {
        private readonly IEmployeeSalaryConfigService _employeeSalaryConfigService;
        private readonly IValidator<EmployeeSalaryConfigDto> _configValidator;
        private readonly IValidator<EmployeeSalaryConfigDetailsDto> _detailsValidator;
        private readonly ISalaryHeadServices _salaryservice;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public EmployeeSalaryConfigController(
            IEmployeeSalaryConfigService employeeSalaryConfigService,
            IValidator<EmployeeSalaryConfigDto> configValidator,
            IValidator<EmployeeSalaryConfigDetailsDto> detailsValidator,
              IConfiguration configuration, IRoleBasedScreenService roleBasedService,
            ISalaryHeadServices salaryservice)
        {
            _employeeSalaryConfigService = employeeSalaryConfigService;
            _configValidator = configValidator;
            _detailsValidator = detailsValidator;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _salaryservice = salaryservice;
        }

        #region EmployeeSalaryConfig

        [HttpGet("GetAllEmployeeSalaryConfig")]
        public async Task<IActionResult> GetAllEmployeeSalaryConfig(string? searchText = null,  string? dropdownFilter = null, DateTime? date = null)
         {
            try
            {
                var configs = await _employeeSalaryConfigService.GetAllConfigs(searchText, dropdownFilter,date);
                if (configs == null || !configs.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDto>>.CreateSuccess(Enumerable.Empty<EmployeeSalaryConfigDto>(), "No configurations found."));
                }
                // Count distinct employees with Approved config
                int approvedCount = configs
                    .Where(x => x.ApprovalStatus == "APPROVED")
                    .Select(x => x.IdEmployee)
                    .Distinct()
                    .Count();
                int submittedCount = configs
                 .Where(x => x.ApprovalStatus == "SUBMITTED" || x.ApprovalStatus == "INTERIM APPROVED")
                 .Select(x => x.IdEmployee)
                 .Distinct()
                 .Count();
                    int rejectedCount = configs
              .Where(x => x.ApprovalStatus == "REJECTED")
              .Select(x => x.IdEmployee)
              .Distinct()
              .Count();
                // Total distinct employees in the result
                int totalEmployees = configs
                    .Select(x => x.IdEmployee)
                    .Distinct()
                    .Count();

                int notApprovedCount = totalEmployees - approvedCount;
                configs.First().ApprovedCount = approvedCount;
                configs.First().NotApprovedCount = notApprovedCount;
                configs.First().submittedCount = submittedCount;
                configs.First().rejectedCount = rejectedCount;
                //configs.First().notConfiguredCount = await _employeeSalaryConfigService.GetNotConfiguredEmployeeCount();
                return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDto>>.CreateSuccess(configs, "EmployeeSalaryConfig retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeSalaryConfigById")]
        public async Task<IActionResult> GetEmployeeSalaryConfigById(int id)
        {
            try
            {
                var config = await _employeeSalaryConfigService.GetConfigById(id);
                if (config == null)
                {
                    return Ok(ApiResponseDto<EmployeeSalaryConfigDto>.CreateSuccess(null, "Configuration not found."));
                }

                return Ok(ApiResponseDto<EmployeeSalaryConfigDto>.CreateSuccess(config, "EmployeeSalaryConfigById retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetAllEmployeeSalaryConfigSp")]
        public async Task<IActionResult> GetAllEmployeeSalaryConfigSp(string? searchText = null, string? dropdownFilter = null, bool isLatest = true)
        {
            try
            {
                var configs = await _employeeSalaryConfigService.GetAllConfigsSp(searchText, dropdownFilter, isLatest);
                if (configs == null || !configs.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDto>>.CreateSuccess(Enumerable.Empty<EmployeeSalaryConfigDto>(), "No configurations found."));
                }

                // Count distinct employees with Approved config
                int approvedCount = configs
                    .Where(x => x.ApprovalStatus == "APPROVED")
                    .Select(x => x.IdEmployee)
                    .Distinct()
                    .Count();
                int submittedCount = configs
                 .Where(x => x.ApprovalStatus == "SUBMITTED" || x.ApprovalStatus == "INTERIM APPROVED")
                 .Select(x => x.IdEmployee)
                 .Distinct()
                 .Count();
                int rejectedCount = configs
              .Where(x => x.ApprovalStatus == "REJECTED")
              .Select(x => x.IdEmployee)
              .Distinct()
              .Count();
                int notConfigCount = configs
        .Where(x => x.ApprovalStatus == "Not Configured")
        .Select(x => x.IdEmployee)
        .Distinct()
        .Count();
                // Total distinct employees in the result
                int totalEmployees = configs
                    .Select(x => x.IdEmployee)
                    .Distinct()
                    .Count();

                int notApprovedCount = totalEmployees - approvedCount;
                configs.First().ApprovedCount = approvedCount;
                configs.First().NotApprovedCount = notApprovedCount;
                configs.First().submittedCount = submittedCount;
                configs.First().rejectedCount = rejectedCount;
                configs.First().notConfiguredCount = notConfigCount;

                return Ok(ApiResponseDto<IEnumerable<EmployeeSalaryConfigDto>>.CreateSuccess(configs, "EmployeeSalaryConfig (SP) retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddEmployeeSalaryConfig")]
        public async Task<IActionResult> AddEmployeeSalaryConfig([FromBody] EmployeeSalaryConfigDto dto)
        {
            var validationResult = await _configValidator.ValidateAsync(dto);
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
            var screenCode = _configuration["ScreenCodes:EmployeeSalaryConfig"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }

            try
            {
                //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _employeeSalaryConfigService.AddConfig(dto,int.Parse(IdEmployee));
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add configuration."));
                return Ok(ApiResponseDto<string>.CreateSuccess("EmployeeSalaryConfig added successfully."));

            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateEmployeeSalaryConfig")]
        public async Task<IActionResult> UpdateEmployeeSalaryConfig([FromBody] EmployeeSalaryConfigDto dto)
        {
            var validationResult = await _configValidator.ValidateAsync(dto);
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
            var screenCode = _configuration["ScreenCodes:EmployeeSalaryConfig"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            if (dto.EmployeeSalaryConfigDetails != null)
            {
                foreach (var detail in dto.EmployeeSalaryConfigDetails)
                {
                    var detailValidationResult = await _detailsValidator.ValidateAsync(detail);
                    if (!detailValidationResult.IsValid)
                    {
                        var detailErrors = string.Join(", ", detailValidationResult.Errors.Select(e => e.ErrorMessage));
                        return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed for Salary Config Details: {detailErrors}"));
                    }
                }
            }

            try
            {

                //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _employeeSalaryConfigService.UpdateConfig(dto, int.Parse(IdEmployee));
                if (result == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Configuration not found."));
                return Ok(ApiResponseDto<string>.CreateSuccess("EmployeeSalaryConfig updated successfully."));

            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

     
    }
}
