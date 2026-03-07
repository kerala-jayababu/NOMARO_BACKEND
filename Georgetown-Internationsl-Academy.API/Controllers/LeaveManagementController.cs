using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    //[Authorize]
    public class LeaveManagementController : ControllerBase
    {

        private readonly ILeaveManaementServices _leaveService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;

        public LeaveManagementController(
            ILeaveManaementServices leaveService,
            IConfiguration configuration,
            IRoleBasedScreenService roleBasedService)
        {
            _leaveService = leaveService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        #region Leave Types

        [HttpGet("GetLeaveTypes")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            try
            {
                var result = await _leaveService.GetLeaveTypes();

                if (!result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveTypesDto>(), "No leave types available."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                    .CreateSuccess(result, "Leave types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeaveTypesEmployee")]
        public async Task<IActionResult> GetLeaveTypesEmployee(int idEmployee, int idYear)
        {
            try
            {
                var result = await _leaveService.GetLeaveTypesEmployee(idEmployee, idYear);

                if (!result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveTypesDto>(), "No leave types available."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveTypesDto>>
                    .CreateSuccess(result, "Leave types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateLeaveTypes")]
        public async Task<IActionResult> AddOrUpdateLeaveTypes([FromBody] LeaveTypesDto leaveTypeDto)
        {
            if (leaveTypeDto == null )
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

            var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idEmployee))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            int loggedInEmployeeId = int.Parse(idEmployee);

            try
            {
                var screenCode = _configuration["ScreenCodes:LeaveTypes"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var success = await _leaveService.AddOrUpdateLeaveTypes(leaveTypeDto);

                if (!success)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save leave types."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave types saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

   
        #region Leave Template
        [HttpGet("GetLeaveTemplates")]
        public async Task<IActionResult> GetLeaveTemplates(int? IdYear, string Status, string? searchText = null)
        {
            try
            {
                var result = await _leaveService.GetLeaveTemplates(IdYear, Status, searchText);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveTemplateDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveTemplateDto>(), "No leave templates found."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveTemplateDto>>
                    .CreateSuccess(result, "Leave templates retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetLeaveTemplateByID")]
        public async Task<IActionResult> GetLeaveTemplateByID(int idLeaveTemplate)
        {
            try
            {
                if (idLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveTemplate is required."));

                var result = await _leaveService.GetLeaveTemplateByID(idLeaveTemplate);

                return Ok(ApiResponseDto<LeaveTemplateDto>
                    .CreateSuccess(result, "Leave template retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
       
        [HttpPost("AddUpdateLeaveTemplate")]
        public async Task<IActionResult> AddUpdateLeaveTemplate([FromBody] LeaveTemplatePostDto dto)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.AddUpdateLeaveTemplate(dto, loggedInEmployeeId);
                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Template saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("DeleteLeaveTemplateDetail")]
        public async Task<IActionResult> DeleteLeaveTemplateDetail(int idLeaveTemplateDetail)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.DeleteLeaveTemplateDetail(idLeaveTemplateDetail);
                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Template Detail deleted successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeesNotConfiguredLeave")]
        public async Task<IActionResult> GetEmployeesNotConfiguredLeave(int idWorkYear)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "V");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.GetEmployeesNotConfiguredLeave(idWorkYear);
                return Ok(ApiResponseDto <IEnumerable<EmployeeNameList>>
                    .CreateSuccess(result, "Leave template retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitLeaveTemplateForApproval")]
        public async Task<IActionResult> SubmitLeaveTemplateForApproval(int idLeaveTemplate)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.SubmitLeaveTemplateForApproval(idLeaveTemplate, loggedInEmployeeId);
                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Template Submitted for Approval."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ApproveLeaveTemplate")]
        public async Task<IActionResult> ApproveLeaveTemplate(int idLeaveTemplate, string approvalStatus)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.ApproveLeaveTemplate(idLeaveTemplate, approvalStatus, loggedInEmployeeId);
                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Template Approved"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeaveTemplateDetailById/{idLeaveTemplateDetails}")]
        public async Task<IActionResult> GetLeaveTemplateDetailById(int idLeaveTemplateDetails)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(loggedInEmployeeId, screenCode, "V"); // View

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.GetLeaveTemplateDetailById(idLeaveTemplateDetails);

                return Ok(ApiResponseDto<LeaveTemplateDetailsDto>
                    .CreateSuccess(result, "Leave template detail retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateLeaveTemplateDetails")]
        public async Task<IActionResult> AddOrUpdateLeaveTemplateDetails([FromBody] LeaveTemplateDetailsDto dto)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(loggedInEmployeeId, screenCode, "A"); // Add/Update

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService
                    .AddOrUpdateLeaveTemplateDetails(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Leave template details saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitEmployeeLeaveConfigForApproval")]
        public async Task<IActionResult> SubmitEmployeeLeaveConfigForApproval(int idEmployeeLeaveTemplate)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfigApproval"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.SubmitEmployeeLeaveConfigForApproval(idEmployeeLeaveTemplate, loggedInEmployeeId);
                return Ok(ApiResponseDto<string>.CreateSuccess("Employee Leave Config Submitted for Approval."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("ApproveEmployeeLeaveConfig")]
        public async Task<IActionResult> ApproveEmployeeLeaveConfig(int IdEmployeeLeaveConfig, string approvalStatus)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfigApproval"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.ApproveEmployeeLeaveConfig(IdEmployeeLeaveConfig, approvalStatus, loggedInEmployeeId);
                return Ok(ApiResponseDto<string>.CreateSuccess("Employee Leave Config Approved"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("ApproveEmployeeLeaveConfigMultiple")]
        public async Task<IActionResult> ApproveEmployeeLeaveConfigMultiple(List<int> IdEmployeeLeaveConfigs, string approvalStatus, string? reason)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfigApproval"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.ApproveEmployeeLeaveConfigMultiple(IdEmployeeLeaveConfigs, approvalStatus, loggedInEmployeeId, reason);
                return Ok(ApiResponseDto<string>.CreateSuccess("Employee Leave Config Approved"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region EmployeeLeaveManagement
        [HttpGet("GetEmployeeLeaveSetup")]
        public async Task<IActionResult> GetEmployeeLeaveSetup(string? searchText, int IdYear, string? ApprovalStatus)
        {
            try
            {
                var result = await _leaveService.GetEmployeesLeaveSetup(searchText, IdYear, ApprovalStatus);
                return Ok(ApiResponseDto<List<EmployeeLeaveSetupDto>>
                    .CreateSuccess(result, "Employee leave setup retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddUpdateEmployeeLeaveConfigWithDetails")]
        public async Task<IActionResult> AddUpdateEmployeeLeaveConfigWithDetails([FromBody] EmployeeLeaveConfigWithDetailsPostDto dto)
        {
            try
            {
                // 🔹 Get logged in user
                var userId = HttpContext.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(
                        ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // 🔹 Permission check
                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfig"];

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("Permission denied."));

                // 🔹 Call Service
                var resultId = await _leaveService
                    .AddUpdateEmployeeLeaveConfigWithDetails(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<int>.CreateSuccess(resultId,
                    "Employee Leave Configuration saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure(
                        "An unexpected error occurred."));
            }
        }


        [HttpGet("GetLeaveSetupOfAnEmployee")]
        public async Task<IActionResult> GetLeaveSetupOfAnEmployee(int IdEmployee, int? IdYear, DateTime? DateTo)
        {
            try
            {

                var result = await _leaveService.GetLeaveSetupOfAnEmployee(IdEmployee, IdYear, DateTo);

                return Ok(ApiResponseDto<EmployeeLeaveSetupDto>
                    .CreateSuccess(result, "Employee leave setup retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeesLeaveConfigStatusDetails")]
        public async Task<IActionResult> GetEmployeesLeaveConfigStatusDetails(int IdYear, int? IdDepartment, int? IdDesignation)
        {
            try
            {

                var result = await _leaveService.GetEmployeesLeaveConfigStatusDetails(IdYear,IdDepartment, IdDesignation);

                return Ok(ApiResponseDto<List<EmpLeaveConfigDetailsDto>>
                    .CreateSuccess(result, "Employee leave setup retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddUpdateEmployeeLeaveConfig")]
        public async Task<IActionResult> AddUpdateEmployeeLeaveConfig([FromBody] EmployeeLeaveConfigsPostDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfig"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var configId = await _leaveService.AddUpdateEmployeeLeaveConfig(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<object>.CreateSuccess(new
                {
                    SuccessFlag = true,
                    Message = "Employee leave config saved successfully.",
                    IdEmployeeLeaveConfig = configId
                }, "Employee leave config saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateEmployeeLeaveConfigDetails")]
        public async Task<IActionResult> AddOrUpdateEmployeeLeaveConfigDetails([FromBody] EmployeeLeaveConfigDetailsPostDto dto)
        {
            try
            {
                // 🔐 Get logged-in user
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>
                        .CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // 🔐 Permission check
                var screenCode = _configuration["ScreenCodes:EmployeeLeaveConfig"];
                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("Permission denied."));

                // ✅ Call service
                await _leaveService.AddUpdateEmployeeLeaveConfigDetails(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Employee leave balance updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {

                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure(
                        "An error occurred while saving employee leave balance."));
            }
        }


        [HttpGet("GetEmployeeLeaveConfigApprovers")]
        public async Task<IActionResult> GetEmployeeLeaveConfigApprovers()
        {
            try
            {
                var userId = HttpContext.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(
                        ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var result = await _leaveService.GetEmployeeLeaveConfigApprovers();

                return Ok(ApiResponseDto<List<int>>
                    .CreateSuccess(result, "Approvers fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure("An unexpected error occurred."));
            }
        }
        [HttpGet("GetLeaveTemplateApprovers")]

        public async Task<IActionResult> GetLeaveTemplateApprovers()
        {
            try
            {
                var userId = HttpContext.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(
                        ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);
                var result = await _leaveService.GetLeaveTemplateApprovers();

                return Ok(ApiResponseDto<List<int>>
                    .CreateSuccess(result, "Approvers fetched successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure("An unexpected error occurred."));
            }
        }
        #endregion


        #region Leave Applications
        [HttpGet("GetLeaveApplications")]
        public async Task<IActionResult> GetLeaveApplications(
            int? idEmployee,
            string? approvalStatus,
            string? applicationStatus,
            DateTime? fromDate,
            DateTime? toDate,
            int? idLeaveType,
            string? SearchText,
            int pageNumber = 1,
            int pageSize = 10,
            string sortBy = "AppliedOn",
            string sortOrder = "DESC")
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var paging = new PagingRequestDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _leaveService.GetLeaveApplications(
                    loggedInEmployeeId,
                    idEmployee,
                    approvalStatus,
                    applicationStatus,
                    fromDate,
                    toDate,
                    idLeaveType,
                    SearchText,
                    paging);

                return Ok(ApiResponseDto<PagedResultDto<LeaveApplicationListDto>>
                    .CreateSuccess(result, "Leave applications retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeaveApplicationsForApproval")]
        public async Task<IActionResult> GetLeaveApplicationsForApproval(string? approvalStatus, string? SearchText, DateTime? fromDate)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var result = await _leaveService.GetLeaveApplicationsForApproval(loggedInEmployeeId, approvalStatus, SearchText, fromDate);


                return Ok(ApiResponseDto<IEnumerable<LeaveApplicationListDto>>
                    .CreateSuccess(result, "Leave templates retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeaveApplication")]
        public async Task<IActionResult> GetLeaveApplication(int idLeaveApplication)
        {
            try
            {
                var data = await _leaveService.GetLeaveApplication(idLeaveApplication);

                return Ok(ApiResponseDto<LeaveApplicationDetailsDto>
                    .CreateSuccess(data, "Leave application retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>
                    .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpPost("AddUpdateLeaveApplication")]
        public async Task<IActionResult> AddUpdateLeaveApplication([FromForm] LeaveApplicationPostDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveApplications"];


                // ✅ Employee can only apply for himself (unless HR permission)
                var canApplyForOthers = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "AA");

                /*
                if (!canApplyForOthers && dto.IdEmployee != loggedInEmployeeId)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You can apply leave only for yourself."));
                */
                var result = await _leaveService.AddUpdateLeaveApplication(dto, loggedInEmployeeId);

                return Ok(ApiResponseDto<LeaveApplicationSaveResultDto>
                    .CreateSuccess(result, result.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitLeaveApplicationApproval")]
        public async Task<IActionResult> SubmitLeaveApplicationApproval(List<int> idChanges,string approvalStatus,string? remarks)
        {
            try
            {
                if (idChanges == null)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveApplications"];


                // ✅ Employee can only apply for himself (unless HR permission)
                var canApplyForOthers = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "AA");

                /*
                if (!canApplyForOthers && dto.IdEmployee != loggedInEmployeeId)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You can apply leave only for yourself."));
                */
                var result = await _leaveService.SubmitLeaveApplicationApproval(idChanges, approvalStatus, remarks, loggedInEmployeeId);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to Submit Approval."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Approved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpDelete("DeleteLeaveApplicationDocument")]
        public async Task<IActionResult> DeleteLeaveApplicationDocument(int idLeaveApplicationDocument)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:LeaveApplications"];

                // ✅ Normal permission for delete (Pending/SentBack)
                var canDelete = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!canDelete)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                // ✅ HR override permission for approved deletes (special code)
                var isHrOverride = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "HRD");

                var result = await _leaveService.DeleteLeaveApplicationDocument(
                    idLeaveApplicationDocument, loggedInEmployeeId, isHrOverride);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to delete document."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Document deleted successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpPost("CancelLeaveApplication")]
        public async Task<IActionResult> CancelLeaveApplication([FromBody] CancelLeaveApplicationDto dto)
        {
            try
            {
                if (dto == null || dto.IdLeaveApplication <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveApplication is required."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var result = await _leaveService.CancelLeaveApplication(
                    dto.IdLeaveApplication,
                    dto.CancelReason,
                    loggedInEmployeeId);

                return Ok(ApiResponseDto<CancelLeaveApplicationResultDto>
                    .CreateSuccess(result, result.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ApplyLeaveTemplateToMultipleEmployees")]
        public async Task<IActionResult> ApplyLeaveTemplateToMultipleEmployees([FromBody] ApplyLeaveTemplateMultileEmployeesDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponseDto<string>
                        .CreateFailure("Request body is required."));

                if (dto.IdLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>
                        .CreateFailure("IdLeaveTemplate is required."));

                if (dto.IdYear <= 0)
                    return BadRequest(ApiResponseDto<string>
                        .CreateFailure("IdYear is required."));

                if (dto.IdEmployees == null || !dto.IdEmployees.Any())
                    return BadRequest(ApiResponseDto<string>
                        .CreateFailure("At least one employee must be selected."));

                var userId = HttpContext.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>
                        .CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var result = await _leaveService
                    .ApplyLeaveTemplateToMultipleEmployees(
                        dto.IdEmployees,
                        dto.IdLeaveTemplate,
                        dto.IdYear,
                        loggedInEmployeeId);

                return Ok(ApiResponseDto<List<LeaveTemplateApplyResult>>
                    .CreateSuccess(result, "Template application processed successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>
                    .CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>
                        .CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetLeaveDashboardEmployee")]
        public async Task<IActionResult> GetLeaveDashboardEmployee(int idEmployee, int idYear)
        {
            var result = await _leaveService.GetLeaveDashboardEmployee(idEmployee, idYear);

            return Ok(result);
        }

        [HttpGet("GetLeaveDashboardEmployeeMonthWise")]
        public async Task<IActionResult> GetLeaveDashboardEmployeeMonthWise(int idEmployee, int idYear)
        {
            try
            {
                var result = await _leaveService.GetLeaveDashboardEmployeeMonthWise(idEmployee, idYear);

                return Ok(result);
            }
            catch(Exception ex)
            {
                return StatusCode(500,
                  ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));

            }
        }

        [HttpGet("GetLeaveApplicationsEmployee")]
        public async Task<IActionResult> GetLeaveApplicationsEmployee(int idEmployee, DateTime FromDate, DateTime ToDate)
        {
            try
            {
                return Ok(await _leaveService
                    .GetLeaveApplicationsEmployee(idEmployee, FromDate, ToDate));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                  ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));

            }
        }

        [HttpGet("CalculateLeaveDaysAsync")]
        public async Task<ActionResult> CalculateLeaveDaysAsync(bool IncludeHoliday, DateTime fromDate, DateTime toDate, bool isHalfDay)
        {
            try
            {
                return Ok(await _leaveService
                    .CalculateLeaveDaysAsync(IncludeHoliday, fromDate, toDate, isHalfDay));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                  ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));

            }
        }
        #endregion

        /// <summary>
        /// Get all documents for a leave application
        /// </summary>
        [HttpGet("GetLeaveApplicationDocuments/{idLeaveApplication}")]
        public async Task<IActionResult> GetLeaveApplicationDocuments(int idLeaveApplication)
        {
            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            if (idLeaveApplication <= 0)
                return BadRequest("Invalid Leave Application Id.");

            var documents = await _leaveService.GetLeaveApplicationDocuments(idLeaveApplication);

            if (documents == null || !documents.Any())
                return NotFound("No documents found for this leave application.");

            return Ok(documents);
        }

        [HttpGet("GetTodaySummary")]
        public async Task<IActionResult> GetTodaySummary(DateTime? date = null)
        {
            try
            {
                var targetDate = (date ?? DateTime.Today).Date;

                var result = await _leaveService.GetTodaySummaryAsync(targetDate);

                if (result == null)
                {
                    return Ok(ApiResponseDto<TodayAtAGlanceDto>
                        .CreateSuccess(null, "No data available for the selected date."));
                }

                return Ok(ApiResponseDto<TodayAtAGlanceDto>
                    .CreateSuccess(result, "Today summary retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetLeaveRequests")]
        public async Task<IActionResult> GetLeaveRequests(DateTime? from = null, DateTime? to = null, int? idDepartment = null,
            int? idDesignation = null, string? status = null, string? searchText = null) 
        {
            try
            {
                var result = await _leaveService.GetLeaveRequestsAsync(
                    from, to, idDepartment, idDesignation, status, searchText);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveRequestRowDto>>
                        .CreateSuccess(Enumerable.Empty<LeaveRequestRowDto>(),
                            "No leave requests found for the given filters."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveRequestRowDto>>
                    .CreateSuccess(result, "Leave requests retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetTodayStatusDetails")]
        public async Task<IActionResult> GetTodayStatusDetails(DateTime Date, string QueryType)
        {
            try
            {
                var result = await _leaveService.GetTodayStatusDetails(Date, QueryType);

                if (result == null)
                {
                    return Ok(ApiResponseDto<IEnumerable<LeaveRequestRowDto>>
                        .CreateSuccess(null, "No data available for the selected date."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeaveRequestRowDto>>
                    .CreateSuccess(result, "Today Details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetNotClockedInWithShiftAsync")]
        public async Task<IActionResult> GetNotClockedInWithShiftAsync(DateTime Date)
        {
            try
            {
                var result = await _leaveService.GetNotClockedInWithShiftAsync(Date);

                if (result == null)
                {
                    return Ok(ApiResponseDto<IEnumerable<NotClockedEmployeeWithShiftDto>>
                        .CreateSuccess(null, "No data available for the selected date."));
                }

                return Ok(ApiResponseDto<IEnumerable<NotClockedEmployeeWithShiftDto>>
                    .CreateSuccess(result, "Details retrieved successfully."));
            }
          catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
    }
}
