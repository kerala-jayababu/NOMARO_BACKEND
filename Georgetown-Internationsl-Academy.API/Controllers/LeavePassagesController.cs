using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class LeavePassagesController : ControllerBase
    {
       
        private readonly ILeavePassageService _leavePassageServices;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IValidator<LeavePassageDto> _leavePassageValidator;

        public LeavePassagesController(ILeavePassageService leavePassageServices, IConfiguration configuration, IRoleBasedScreenService roleBasedService, IValidator<LeavePassageDto> leavePassageValidator)
        {
            _leavePassageServices = leavePassageServices;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _leavePassageValidator = leavePassageValidator;
        }


        [HttpGet("GetLeavePassagesList")]
        public async Task<IActionResult> GetLeavePassagesList(string? searchText,string? dropdownFilter = null)
        {
            try
            {
                

                var leavePassageList = await _leavePassageServices.GetLeavePassagesList( searchText, dropdownFilter);

                if (leavePassageList == null || !leavePassageList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeavePassageDto>>.CreateSuccess(Enumerable.Empty<LeavePassageDto>(), "No LeavePassage found."));
                }
               

                return Ok(ApiResponseDto<IEnumerable<LeavePassageDto>>.CreateSuccess(leavePassageList, "LeavePassage retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeavePassageRequestsForHR")]
        public async Task<IActionResult> GetLeavePassageRequestsForHR(int idWorkYear, string requestStatus, string? searchText, string? approvalStatus)
        {
            try
            {


                var leavePassageList = await _leavePassageServices.GetLeavePassageRequestsForHR(idWorkYear, requestStatus, searchText, approvalStatus);

                if (leavePassageList == null || !leavePassageList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeavePassageForHRDto>>.CreateSuccess(Enumerable.Empty<LeavePassageForHRDto>(), "No LeavePassage found."));
                }


                return Ok(ApiResponseDto<IEnumerable<LeavePassageForHRDto>>.CreateSuccess(leavePassageList, "LeavePassage retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeavePassagesByemployeeId")]
        public async Task<IActionResult> GetLeavePassagesListByemployeeId(int EmployeeId)
        {
            try
            {


                var leavePassageList = await _leavePassageServices.GetLeavePassagesListByemployeeId(EmployeeId);

                if (leavePassageList == null || !leavePassageList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeavePassageDto>>.CreateSuccess(Enumerable.Empty<LeavePassageDto>(), "No LeavePassage found."));
                }


                return Ok(ApiResponseDto<IEnumerable<LeavePassageDto>>.CreateSuccess(leavePassageList, "LeavePassage retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetLeavePassageById")]
        public async Task<IActionResult> GetLeavePassageById(int id)
        {
            try
            {
                var leavePassage = await _leavePassageServices.GetLeavePassagesById(id);

                if (leavePassage == null)
                {
                    return Ok(ApiResponseDto<LeavePassageDto>.CreateSuccess(null, "Leave Passage not found."));
                }


                return Ok(ApiResponseDto<LeavePassageDto>.CreateSuccess(leavePassage, "Leave Passage retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddLeavePassage")]
        public async Task<IActionResult> AddLeavePassage(LeavePassageDto dto)
        {
            var validationResult = await _leavePassageValidator.ValidateAsync(dto);
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
            //var screenCode = _configuration["ScreenCodes:OvertimeTransactions"];
            //var actionType = "A";

            //// Check permission
            //var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            //if (!hasPermission)
            //{
            //    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            //}

            try
            {               
                var result = await _leavePassageServices.AddLeavePassages(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add leave Passage."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Passage added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UpdateLeavePassage")]
        public async Task<IActionResult> UpdateLeavePassage(LeavePassageDto dto)
        {
            var validationResult = await _leavePassageValidator.ValidateAsync(dto);
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
            //var screenCode = _configuration["ScreenCodes:OvertimeTransactions"];
            //var actionType = "U";

            //// Check permission
            //var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            //if (!hasPermission)
            //{
            //    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            //}
            try
            {
                //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _leavePassageServices.UpdateLeavePassage(dto, int.Parse(IdEmployee));
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update Leave Passage."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave Passage updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #region LeavePassageAmount
        [HttpGet("GetLeavePassageAmountDetails")]
        public async Task<IActionResult> GetLeavePassageAmountDetails(int? financialYear = null, string? searchString = null)
        {
            try
            {
                var result = await _leavePassageServices.GetLeavePassageAmountDetails(financialYear, searchString);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<LeavePassageAmountDto>>.CreateSuccess(Enumerable.Empty<LeavePassageAmountDto>(), "No records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<LeavePassageAmountDto>>.CreateSuccess(result, "Leave Passage details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitLeavePassage")]
        public async Task<IActionResult> SubmitLeavePassage([FromBody] List<LeavePassageAmountDetailsDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid data."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            try
            {
                var success = await _leavePassageServices.SubmitLeavePassageAsync(dtoList, Convert.ToInt32(IdEmployee));

                if (!success)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save Leave Passage details."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave passage details saved successfully."));
            }
            catch (Exception ex)
            {                
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitLeavePassageReversal")]
        public async Task<IActionResult> SubmitLeavePassageReversal([FromBody] List<LeavePassageReversalDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid data."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            try
            {
                var success = await _leavePassageServices.SubmitLeavePassageReversalAsync(dtoList, Convert.ToInt32(IdEmployee));

                if (!success)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save Leave Passage details."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave passage details saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("SubmitLeavePassageAddition")]
        public async Task<IActionResult> SubmitLeavePassageAddition([FromBody] List<LeavePassageAdditionDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid data."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            try
            {
                var success = await _leavePassageServices.SubmitLeavePassageAdditionAsync(dtoList, Convert.ToInt32(IdEmployee));

                if (!success)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save Leave Passage details."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Leave passage details saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetCurrentWorkYearMonths")]
        public async Task<IActionResult> GetCurrentWorkYearMonths()
        {
            try
            {
                var result = await _leavePassageServices.GetCurrentWorkYearMonths();

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<WorkMonthsInYearDto>>.CreateSuccess(Enumerable.Empty<WorkMonthsInYearDto>(), "No records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<WorkMonthsInYearDto>>.CreateSuccess(result, "Months retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

    }
}
