using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("AddOrUpdateLeaveTypes")]
        public async Task<IActionResult> AddOrUpdateLeaveTypes([FromBody] List<LeaveTypesDto> leaveTypeDtos)
        {
            if (leaveTypeDtos == null || !leaveTypeDtos.Any())
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

                var success = await _leaveService.AddOrUpdateLeaveTypes(leaveTypeDtos);

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

        #region Annual Leave Config

        [HttpGet("GetAnnualLeaveTypeConfigs")]
        public async Task<IActionResult> GetAnnualLeaveTypeConfigs(int? idAnnualLeaveTypeConfig,int? idLeaveType,int? idYear,bool? isActive)
        {
            try
            {
                var result = await _leaveService.GetAnnualLeaveTypeConfigs(
                    idAnnualLeaveTypeConfig,
                    idLeaveType,
                    idYear,
                    isActive
                );

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<AnnualLeaveTypeConfigDto>>
                        .CreateSuccess(Enumerable.Empty<AnnualLeaveTypeConfigDto>(), "No annual leave configs found."));
                }

                return Ok(ApiResponseDto<IEnumerable<AnnualLeaveTypeConfigDto>>
                    .CreateSuccess(result, "Annual leave configuration retrieved."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddUpdateAnnualLeaveTypeConfig")]
        public async Task<IActionResult> AddUpdateAnnualLeaveTypeConfig([FromBody] List<AnnualLeaveTypeConfigDto> configDtos)
        {
            if (configDtos == null || !configDtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            int loggedInEmployeeId = int.Parse(userId);

            try
            {
                var screenCode = _configuration["ScreenCodes:AnnualLeaveConfig"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var ids = await _leaveService.AddUpdateAnnualLeaveTypeConfig(configDtos, loggedInEmployeeId);

                return Ok(ApiResponseDto<object>.CreateSuccess(new
                {
                    SuccessFlag = true,        
                    Message = "Annual leave configuration saved successfully.",
                    IdAnnualLeaveTypeConfig = ids
                }, "Annual leave configuration saved successfully."));
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
[HttpPost("DeactivateAnnualLeaveTypeConfig")]
        public async Task<IActionResult> DeactivateAnnualLeaveTypeConfig(int idAnnualLeaveTypeConfig)
        {
            try
            {
                if (idAnnualLeaveTypeConfig <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdAnnualLeaveTypeConfig is required."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:AnnualLeaveConfig"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var result = await _leaveService.DeactivateAnnualLeaveTypeConfig(idAnnualLeaveTypeConfig, loggedInEmployeeId);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to deactivate annual leave configuration."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Annual leave configuration deactivated successfully."));
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
        public async Task<IActionResult> GetLeaveTemplates(int? idLeaveTemplate, bool? isActive, string? searchText)
        {
            try
            {
                var result = await _leaveService.GetLeaveTemplates(idLeaveTemplate, isActive, searchText);

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
        [HttpGet("GetLeaveTemplate")]
        public async Task<IActionResult> GetLeaveTemplate(int idLeaveTemplate)
        {
            try
            {
                if (idLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveTemplate is required."));

                var result = await _leaveService.GetLeaveTemplate(idLeaveTemplate);

                return Ok(ApiResponseDto<LeaveTemplateWithDetailsDto>
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

                return Ok(ApiResponseDto<LeaveTemplateSaveResponseDto>
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
        [HttpPost("DeactivateLeaveTemplate")]
        public async Task<IActionResult> DeactivateLeaveTemplate(int idLeaveTemplate)
        {
            try
            {
                if (idLeaveTemplate <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdLeaveTemplate is required."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:LeaveTemplates"];
                var hasPermission = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D");

                if (!hasPermission)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                var success = await _leaveService.DeactivateLeaveTemplate(idLeaveTemplate, loggedInEmployeeId);

                if (!success)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to deactivate leave template."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave template deactivated successfully."));
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

        #region EmployeeLeaveManagement
        [HttpGet("GetEmployeeLeaveSetup")]
        public async Task<IActionResult> GetEmployeeLeaveSetup(int idEmployee, DateTime? activeOnDate)
        {
            try
            {
                if (idEmployee <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdEmployee is mandatory."));

                var result = await _leaveService.GetEmployeeLeaveSetup(idEmployee, activeOnDate);

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
        [HttpPost("AddUpdateEmployeeLeaveConfig")]
        public async Task<IActionResult> AddUpdateEmployeeLeaveConfig([FromBody] EmployeeLeaveConfigPostDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeLeaveSetup"];
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
        [Consumes("multipart/form-data")]
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

                // ✅ Employee Apply Permission
                var canApply = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A");
                if (!canApply)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

                // ✅ Employee can only apply for himself (unless HR permission)
                var canApplyForOthers = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "AA");

                if (!canApplyForOthers && dto.IdEmployee != loggedInEmployeeId)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You can apply leave only for yourself."));

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

                var screenCode = _configuration["ScreenCodes:LeaveApplications"];

                // ✅ Cancel permission
                var canCancel = await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "U");
                if (!canCancel)
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("Permission denied."));

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



        #endregion
    }
}
