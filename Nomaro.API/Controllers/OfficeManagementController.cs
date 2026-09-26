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
    public class OfficeManagementController : ControllerBase
    {
        private readonly IOfficeManagementService _officeManagementService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IValidator<OfficeTypeDto> _officeTypeValidator;
        private readonly IValidator<OfficeDto> _officeValidator;
        private readonly IValidator<EmployeeOfficePostingDto> _employeeOfficePostingValidator;

        public OfficeManagementController(
            IOfficeManagementService officeManagementService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService,
            IValidator<OfficeTypeDto> officeTypeValidator,
            IValidator<OfficeDto> officeValidator,
            IValidator<EmployeeOfficePostingDto> employeeOfficePostingValidator)
        {
            _officeManagementService = officeManagementService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _officeTypeValidator = officeTypeValidator;
            _officeValidator = officeValidator;
            _employeeOfficePostingValidator = employeeOfficePostingValidator;
        }

        #region Office Types

        [HttpGet("GetOfficeTypes")]
        public async Task<IActionResult> GetOfficeTypes(bool? isActive)
        {
            try
            {
                var officeTypes = await _officeManagementService.GetOfficeTypes(isActive);

                if (officeTypes == null || !officeTypes.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<OfficeTypeDto>>
                        .CreateSuccess(Enumerable.Empty<OfficeTypeDto>(), "No office types available."));
                }

                return Ok(ApiResponseDto<IEnumerable<OfficeTypeDto>>
                    .CreateSuccess(officeTypes, "Office types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateOfficeTypes")]
        public async Task<IActionResult> AddOrUpdateOfficeTypes([FromBody] OfficeTypeDto officeTypeDto)
        {
            var validationResult = await _officeTypeValidator.ValidateAsync(officeTypeDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OfficeManagement"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                if (await _officeManagementService.IsOfficeTypeCodeExists(officeTypeDto.OfficeTypeCode, officeTypeDto.IdOfficeType))
                {
                    return Conflict(ApiResponseDto<string>
                        .CreateFailure("Office type code already exists."));
                }

                var isSuccess = await _officeManagementService.AddOrUpdateOfficeTypes(officeTypeDto, int.Parse(IdEmployee));

                if (!isSuccess)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update office type."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Office type added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Offices

        [HttpGet("GetOffices")]
        public async Task<IActionResult> GetOffices(string? searchText, int? idOfficeType, int? idParentOffice, bool? isActive)
        {
            try
            {
                var offices = await _officeManagementService.GetOffices(searchText, idOfficeType, idParentOffice, isActive);

                if (offices == null || !offices.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<OfficeDto>>
                        .CreateSuccess(Enumerable.Empty<OfficeDto>(), "No offices available."));
                }

                return Ok(ApiResponseDto<IEnumerable<OfficeDto>>
                    .CreateSuccess(offices, "Offices retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetOfficeById")]
        public async Task<IActionResult> GetOfficeById(int idOffice)
        {
            if (idOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            try
            {
                var office = await _officeManagementService.GetOfficeById(idOffice);

                if (office == null)
                {
                    return Ok(ApiResponseDto<OfficeDto>.CreateSuccess(null, "Office not found."));
                }

                return Ok(ApiResponseDto<OfficeDto>.CreateSuccess(office, "Office retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOffice")]
        public async Task<IActionResult> AddOffice([FromBody] OfficeDto officeDto)
        {
            var validationResult = await _officeValidator.ValidateAsync(officeDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OfficeManagement"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                if (await _officeManagementService.IsOfficeCodeExists(officeDto.OfficeCode, 0))
                {
                    return Conflict(ApiResponseDto<string>
                        .CreateFailure("Office code already exists."));
                }

                var idOffice = await _officeManagementService.AddOffice(officeDto, int.Parse(IdEmployee));

                if (idOffice <= 0)
                {
                    return UnprocessableEntity(ApiResponseDto<string>
                        .CreateFailure("Failed to add office."));
                }

                return Ok(ApiResponseDto<int>
                    .CreateSuccess(idOffice, "Office added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateOffice")]
        public async Task<IActionResult> UpdateOffice([FromBody] OfficeDto officeDto)
        {
            if (officeDto == null || officeDto.IdOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            var validationResult = await _officeValidator.ValidateAsync(officeDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OfficeManagement"];
                var actionType = "U";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                if (await _officeManagementService.IsOfficeCodeExists(officeDto.OfficeCode, officeDto.IdOffice))
                {
                    return Conflict(ApiResponseDto<string>
                        .CreateFailure("Another office with same office code exists."));
                }

                var isSuccess = await _officeManagementService.UpdateOffice(officeDto, int.Parse(IdEmployee));

                if (!isSuccess)
                {
                    return NotFound(ApiResponseDto<string>
                        .CreateFailure("Office not found."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Office updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateOfficeStatus")]
        public async Task<IActionResult> UpdateOfficeStatus(int idOffice, bool isActive)
        {
            if (idOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OfficeManagement"];
                var actionType = "U";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                var isSuccess = await _officeManagementService.UpdateOfficeStatus(idOffice, isActive, int.Parse(IdEmployee));

                if (!isSuccess)
                {
                    return NotFound(ApiResponseDto<string>
                        .CreateFailure("Office not found."));
                }

                return Ok(ApiResponseDto<string>
                    .CreateSuccess(isActive ? "Office activated successfully." : "Office deactivated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Employee Office Postings

        [HttpGet("GetOfficeEmployees")]
        public async Task<IActionResult> GetOfficeEmployees(int idOffice, bool isCurrentOnly = true)
        {
            if (idOffice <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
            }

            try
            {
                var employees = await _officeManagementService.GetOfficeEmployees(idOffice, isCurrentOnly);

                if (employees == null || !employees.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<OfficeEmployeeDto>>
                        .CreateSuccess(Enumerable.Empty<OfficeEmployeeDto>(), "No employees posted to this office."));
                }

                return Ok(ApiResponseDto<IEnumerable<OfficeEmployeeDto>>
                    .CreateSuccess(employees, "Office employees retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeOfficePostings")]
        public async Task<IActionResult> GetEmployeeOfficePostings(int idEmployee)
        {
            if (idEmployee <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid employee ID."));
            }

            try
            {
                var postings = await _officeManagementService.GetEmployeeOfficePostings(idEmployee);

                if (postings == null || !postings.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeOfficePostingDto>>
                        .CreateSuccess(Enumerable.Empty<EmployeeOfficePostingDto>(), "No office postings found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeOfficePostingDto>>
                    .CreateSuccess(postings, "Office postings retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateEmployeeOfficePosting")]
        public async Task<IActionResult> AddOrUpdateEmployeeOfficePosting([FromBody] EmployeeOfficePostingDto postingDto)
        {
            var validationResult = await _employeeOfficePostingValidator.ValidateAsync(postingDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>
                    .CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:OfficeManagement"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("You do not have permission to perform this action."));
                }

                if (await _officeManagementService.IsEmployeeOfficePostingExists(postingDto))
                {
                    return Conflict(ApiResponseDto<string>
                        .CreateFailure("Employee is already posted to this office from the same date."));
                }

                var idEmployeeOfficePosting = await _officeManagementService
                    .AddOrUpdateEmployeeOfficePosting(postingDto, int.Parse(IdEmployee));

                if (idEmployeeOfficePosting <= 0)
                {
                    return StatusCode(500,
                        ApiResponseDto<string>.CreateFailure("Failed to add or update employee office posting."));
                }

                return Ok(ApiResponseDto<int>
                    .CreateSuccess(idEmployeeOfficePosting, "Employee office posting added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion
    }
}
