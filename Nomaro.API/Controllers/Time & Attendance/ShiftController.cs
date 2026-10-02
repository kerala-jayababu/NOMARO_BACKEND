using Asp.Versioning;
using FluentValidation;
using Nomaro.API.Services.Implementations.TimeAndAttendance;
using Nomaro.API.DTO;
using Nomaro.API.DTO.Shift;
using Nomaro.API.DTO.Time___Attendance.Shift;
using Nomaro.API.Models;
using Nomaro.API.Services.Implimentation.Time___Attendance.Shift;
using Nomaro.API.Services.Interface;
using Nomaro.API.Services.Interface.Shift;
using Nomaro.API.Services.Interface.Time___Attendance.Shift;
using iText.Kernel.XMP.Impl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nomaro.API.Controllers.Time___Attendance
{
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]

    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _shiftService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedScreenService;
        private readonly IShiftScheduleService _shiftScheduleService;
        private readonly IShiftEmployeeService _shiftEmployeeService;
        private readonly IShiftAssignmentService _shiftAssignmentService;
        private readonly IShiftSetupService _shiftSetupService;
        private readonly IValidator<ShiftDto> _shiftvalidator;
        private readonly IValidator<ShiftScheduleDto> _shiftScheduleValidator;
        private readonly IValidator<ApproveTimesheetDto> _appproveTimeSheetValidator;
        private readonly IValidator<UpdateClockInOutMissingEntryDto> _updateClockInOutMissingEntryValidator;
        private readonly IValidator<UpdateShortTimeReasonDto> _UpdateShortTimeReasonDtoValidator;
        
        public ShiftController(IShiftService shiftService, IConfiguration configuration,
            IRoleBasedScreenService roleBasedScreenService,
            IShiftScheduleService shiftScheduleService,
            IShiftEmployeeService shiftEmployeeService,
            IShiftAssignmentService shiftAssignmentService,
            IShiftSetupService shiftSetupService,
            // Injecting validators
            IValidator<ShiftDto> shiftValidator,
            IValidator<ApproveTimesheetDto> appproveTimeSheetValidator,
            IValidator<UpdateClockInOutMissingEntryDto> updateClockInOutMissingEntryValidator,
             IValidator<UpdateShortTimeReasonDto> updateShortTimeReasonDtoValidator,
             IValidator<ShiftScheduleDto> shiftScheduleValidator)
        {
            _shiftService = shiftService;
            _configuration = configuration;
            _shiftvalidator = shiftValidator;
            _shiftEmployeeService = shiftEmployeeService;
            _shiftAssignmentService = shiftAssignmentService;
            _shiftSetupService = shiftSetupService;
            _shiftScheduleValidator = shiftScheduleValidator;
            _appproveTimeSheetValidator = appproveTimeSheetValidator;
            _UpdateShortTimeReasonDtoValidator = updateShortTimeReasonDtoValidator;
            _updateClockInOutMissingEntryValidator = updateClockInOutMissingEntryValidator;
            _shiftScheduleService = shiftScheduleService;
            _roleBasedScreenService = roleBasedScreenService;

        }
        #region ShiftDefinitions


        [HttpGet("GetShiftList")]
        public async Task<IActionResult> GetShiftList(int? idOffice)
        {
            try
            {
                if (idOffice.HasValue)
                {
                    if (idOffice <= 0)
                    {
                        return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid office ID."));
                    }

                    var employeeIdClaim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
                    {
                        return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
                    }

                    var visibleOffices = await _shiftSetupService.GetShiftSetupDetails(employeeId);
                    if (!visibleOffices.Any(office => office.IdOffice == idOffice.Value))
                    {
                        return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have access to this office."));
                    }
                }

                var shiftList = await _shiftService.GetShiftList(idOffice);

                if (shiftList == null || !shiftList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ShiftDto>>.CreateSuccess(Enumerable.Empty<ShiftDto>(), "No shifts found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ShiftDto>>.CreateSuccess(shiftList, "Shift list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetShiftById")]
        public async Task<IActionResult> GetShiftById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid shift ID."));
            }

            try
            {
                var shift = await _shiftService.GetShiftById(id);

                if (shift == null)
                {
                    return Ok(ApiResponseDto<ShiftDto>.CreateSuccess(null, "Shift not found."));
                }

                return Ok(ApiResponseDto<ShiftDto>.CreateSuccess(shift, "Shift retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("AddShift")]
        public async Task<IActionResult> AddShift([FromBody] ShiftDto dto)
        {
            try
            {
                if (dto.IdOffice is null or <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Office ID is required."));
                }

                var validationResult = await _shiftvalidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeId, out var authenticatedEmployeeId) || authenticatedEmployeeId <= 0)
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                var visibleOffices = await _shiftSetupService.GetShiftSetupDetails(authenticatedEmployeeId);
                if (!visibleOffices.Any(office => office.IdOffice == dto.IdOffice.Value))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have access to this office."));

                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "A");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var existing = (await _shiftService.GetShiftList(dto.IdOffice))
                    .FirstOrDefault(s => s.ShiftName.Equals(dto.ShiftName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    return Conflict(ApiResponseDto<string>.CreateFailure("Shift already exists."));

                var result = await _shiftService.AddShift(dto);
                if (result == null)
                    return UnprocessableEntity(ApiResponseDto<string>.CreateFailure("Failed to add shift."));

                return Ok(ApiResponseDto<ShiftDto>.CreateSuccess(result, "Shift added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UpdateShift")]
        public async Task<IActionResult> UpdateShift([FromBody] ShiftDto dto)
        {
            try
            {
                if (dto == null || dto.IdShift == null || dto.IdShift <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid shift ID."));
                }

                var validationResult = await _shiftvalidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeId, out var authenticatedEmployeeId) || authenticatedEmployeeId <= 0)
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                if (dto.IdOffice is null or <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Office ID is required."));

                var visibleOffices = await _shiftSetupService.GetShiftSetupDetails(authenticatedEmployeeId);
                if (!visibleOffices.Any(office => office.IdOffice == dto.IdOffice.Value))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have access to this office."));

                var existingShift = await _shiftService.GetShiftById(dto.IdShift);
                if (existingShift == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Shift not found."));
                if (existingShift.IdOffice != dto.IdOffice)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("A shift cannot be moved to a different office."));

                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "U");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var duplicate = (await _shiftService.GetShiftList(dto.IdOffice))
                    .FirstOrDefault(s => s.ShiftName.Equals(dto.ShiftName, StringComparison.OrdinalIgnoreCase) && s.IdShift != dto.IdShift);
                if (duplicate != null)
                    return Conflict(ApiResponseDto<string>.CreateFailure("Another shift with same name exists."));

                var result = await _shiftService.UpdateShift(dto);
                if (result == null)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update shift."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Shift updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region ShiftSchedules

        [HttpGet("GetShiftScheduleList")]
        public async Task<IActionResult> GetShiftScheduleList(int idShift)
        {
            if (idShift <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid IdShift."));
            }
            try
            {
                var list = await _shiftScheduleService.GetAllShiftSchedulesAsync(idShift);
                if (list == null || !list.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ShiftScheduleDto>>.CreateSuccess(Enumerable.Empty<ShiftScheduleDto>(), "No shift schedules found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ShiftScheduleDto>>.CreateSuccess(list, "Shift schedule list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageShiftSchedules")]
        public async Task<IActionResult> ManageShiftSchedules(List<ShiftScheduleDto> shiftSchedules)
        {
            if (shiftSchedules == null || !shiftSchedules.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Shift schedule list cannot be null or empty."));
            }

            var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "A");
            // if (!hasPermission)
            //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));
            var validationErrors = new List<string>();

            // Validate each DTO using FluentValidation
            foreach (var dto in shiftSchedules)
            {
                var validationResult = await _shiftScheduleValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    validationErrors.Add($"Shift (IdShift: {dto.IdShift}, Start: {dto.StartTime}, End: {dto.EndTime}): {errorMessage}");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {string.Join(" | ", validationErrors)}"));
            }
            try
            {
                var result = await _shiftScheduleService.ManageShiftSchedulesAsync(shiftSchedules);
                if (result == null || !result.Any())
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage shift schedules."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Shift schedules managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        #endregion

        #region ShiftEmployees
        [HttpGet("GetShiftEmployeesByShift")]
        public async Task<IActionResult> GetShiftEmployeesByShift(int idShift)
        {
            if (idShift <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid IdShift."));

            try
            {
                var result = await _shiftEmployeeService.GetShiftEmployeesByShiftAsync(idShift);
                return Ok(ApiResponseDto<List<ShiftEmployeeDto>>.CreateSuccess(result, "Shift employees retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageShiftEmployees")]
        public async Task<IActionResult> ManageShiftEmployees(List<ShiftEmployeeDto> employees)
        {

            if (employees == null || !employees.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee list cannot be empty."));

            try
            {
                var result = await _shiftEmployeeService.ManageShiftEmployeesAsync(employees);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Shift employees managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region ShiftAssignment
        [HttpGet("GetShiftAssignmentsByShift")]
        public async Task<IActionResult> GetShiftAssignmentsByShift(int idShift)
        {
            if (idShift <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid IdShift."));

            try
            {
                var result = await _shiftAssignmentService.GetShiftAssignmentsByShiftAsync(idShift);
                return Ok(ApiResponseDto<List<ShiftAssignmentDto>>.CreateSuccess(result, "Shift assignments retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageShiftAssignments")]
        public async Task<IActionResult> ManageShiftAssignments([FromBody] List<ShiftAssignmentDto> assignments)
        {
            if (assignments == null || !assignments.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Assignment list cannot be empty."));

            try
            {
                var result = await _shiftAssignmentService.ManageShiftAssignmentsAsync(assignments);
                return Ok(ApiResponseDto<List<ShiftAssignmentDto>>.CreateSuccess(null, "Shift assignments managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("DeleteShiftAssignment")]
        public async Task<IActionResult> DeleteShiftAssignment(int IdShiftAssignment, int IdEmployee)
        {
            
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);
                var result = await _shiftAssignmentService.DeleteShiftAssignment(IdShiftAssignment, IdEmployee, loggedInEmployeeId);
                return Ok(ApiResponseDto<List<ShiftAssignmentDto>>.CreateSuccess(null, "Shift assignment Deleted"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpPost("DeleteShiftAssignmentOfASchedule")]
        public async Task<IActionResult> DeleteShiftAssignmentOfASchedule(int IdShiftSchedule, DateTime ShiftDateTime)
        {

            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);
                var result = await _shiftAssignmentService.DeleteShiftAssignmentOfASchedule(IdShiftSchedule, ShiftDateTime, loggedInEmployeeId);
                return Ok(ApiResponseDto<List<ShiftAssignmentDto>>.CreateSuccess(null, "Shift assignment Deleted"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("CopyShiftAssignmentsByDateAsync")]
        public async Task<IActionResult> CopyShiftAssignmentsByDateAsync([FromBody] CopyShiftAssignmentsRequest request)
        {

            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);
                var result = await _shiftAssignmentService.CopyShiftAssignmentsByDateAsync(request.IdShif, request.SourceDate, request.TargetDate);
                return Ok(ApiResponseDto<List<ShiftAssignmentDto>>.CreateSuccess(null, "Shift assignment Copied"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

        #region ClockInClockOutDeatils
        [HttpGet("GetClockInClockOutDetails")]
        public async Task<IActionResult> GetClockInClockOutDetails(string? idEmployee, DateTime? dateFrom,DateTime? dateTo,int? idDepartment,bool? missingEntryOnly)
        {
            if (!dateFrom.HasValue)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Start date (dateFrom) is required."));

            if (!dateTo.HasValue)
                return BadRequest(ApiResponseDto<string>.CreateFailure("End date (dateTo) is required."));

            try
            {
                var details = await _shiftService.GetClockInClockOutDetailsAsync(
                    string.IsNullOrWhiteSpace(idEmployee) ? null : idEmployee,
                    dateFrom.Value,
                    dateTo.Value,
                    idDepartment,
                    missingEntryOnly);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ClockInOutDto>>.CreateSuccess(
                        Enumerable.Empty<ClockInOutDto>(), 
                        "No clock-in/out records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ClockInOutDto>>.CreateSuccess(details, "Clock-in/out details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateClockInOutMissingEntries")]
        public async Task<IActionResult> UpdateClockInOutMissingEntries([FromBody] List<UpdateClockInOutMissingEntryDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("No entries provided."));

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            foreach (var dto in dtos)
            {
                var validationResult = await _updateClockInOutMissingEntryValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join(" | ", validationResult.Errors.Select(e => $"ID {dto.IdClockDetail}: {e.ErrorMessage}"));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errorMessages}"));
                }
            }

            var success = await _shiftService.UpdateClockInOutMissingEntriesAsync(dtos,int.Parse(IdEmployee));
            if (!success)
                return BadRequest(ApiResponseDto<string>.CreateFailure("One or more records could not be updated."));

            return Ok(ApiResponseDto<string>.CreateSuccess("All entries updated successfully."));
        }
        [HttpPost("ForgotAccessCardMissingEntry")]
        public async Task<IActionResult> ForgotAccessCardMissingEntry([FromBody] ForgotAccessCardMissingEntryDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponseDto<string>.CreateFailure("No entries provided."));

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var success = await _shiftService.ForgotAccessCardMissingEntry(dto, Convert.ToInt32(IdEmployee));

                if (!success)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("One or more records could not be updated."));

                string msg = "";
                if (dto.EntryType == "IN")
                    msg = "Your Entry Time Saved Successfully. Please Remember to Update Exit Time When You Leave";
                else
                    msg = "Your Exit Time Saved Successfully";

                return Ok(ApiResponseDto<string>.CreateSuccess(msg));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("An error occurred while processing the request."));
            }
        }


        #endregion

        #region Dayttendance
        [HttpGet("GetDayAttendanceDetails")]
        public async Task<IActionResult> GetDayAttendanceDetails(DateTime dateFrom , DateTime dateTo , string? idEmployee = null, int? idDepartment = null)
        {

            if (dateFrom == default || dateTo == default)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Both 'dateFrom' and 'dateTo' are required."));
            List<int> employeeIds = new();
            if (!string.IsNullOrWhiteSpace(idEmployee))
            {
                try
                {
                    employeeIds = idEmployee.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(id => int.Parse(id.Trim()))
                                            .ToList();
                }
                catch
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid format for 'idEmployee'. It should be a comma-separated list of integers."));
                }
            }
            try
            {
                var data = await _shiftService.GetDayAttendanceDetails(dateFrom, dateTo, employeeIds, idDepartment);

                if (data == null || !data.Any())
                    return Ok(ApiResponseDto<IEnumerable<DayAttendanceDto>>.CreateSuccess(Enumerable.Empty<DayAttendanceDto>(), "No attendance records found."));

                return Ok(ApiResponseDto<IEnumerable<DayAttendanceDto>>.CreateSuccess(data, "Day attendance retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ApproveTimesheets")]
        public async Task<IActionResult> ApproveTimesheets([FromBody] List<ApproveTimesheetDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("No timesheet entries provided."));

            var invalidDtos = dtos.Where(d => d.IdDayAttendance <= 0).ToList();
            if (invalidDtos.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("One or more timesheet entries have invalid IDs."));

            foreach (var dto in dtos)
            {
                var validationResult = await _appproveTimeSheetValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed for ID {dto.IdDayAttendance}: {errors}"));
                }
            }

            var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            try
            {
                var result = await _shiftService.ApproveTimesheetAsync(dtos, int.Parse(employeeId));
                if (!result)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("One or more timesheet entries could not be updated."));

                return Ok(ApiResponseDto<string>.CreateSuccess("All timesheet entries approved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure("An error occurred while processing the request."));
            }
        }

        [HttpPost("UpdateAttendanceShortTimeDetails")]
        public async Task<IActionResult> UpdateAttendanceShortTimeDetails([FromBody] UpdateShortTimeReasonDto dto)
        {
            var validationResult = await _UpdateShortTimeReasonDtoValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure(
                    string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage))
                ));
            }

            try
            {
                var result = await _shiftService.UpdateAttendanceShortTimeDetailsAsync(dto);

                if (!result)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Attendance record not found or could not be updated."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Short time reason updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetClockInClockOutDetailsOfEmployeeGroupedByDate")]
        public async Task<IActionResult> GetClockInClockOutDetailsOfEmployeeGroupedByDate(int idEmployee, DateTime dateFrom, DateTime dateTo)
        {

            try
            {
                var details = await _shiftService.GetClockInClockOutDetailsOfEmployeeGroupedByDate(idEmployee,dateFrom,dateTo);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ClockInOutDetailsDateGroupedDto>>.CreateSuccess(
                        Enumerable.Empty<ClockInOutDetailsDateGroupedDto>(),
                        "No clock-in/out records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ClockInOutDetailsDateGroupedDto>>.CreateSuccess(details, "Clock-in/out details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetMissingEntryDetailsForApproval")]
        public async Task<IActionResult> GetMissingEntryDetailsForApproval(DateTime? dateFrom, string? approvalStatus)
        {
            try
            {
                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                var details = await _shiftService.GetMissingEntryDetailsForApproval(Convert.ToInt32( employeeId), dateFrom, approvalStatus);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<MissingEntryForApprovalDto>>.CreateSuccess(
                        Enumerable.Empty<MissingEntryForApprovalDto>(),
                        "No clock-in/out records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<MissingEntryForApprovalDto>>.CreateSuccess(details, "Clock-in/out details for approval retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("TogglingMissingEntry")]
        public async Task<IActionResult> TogglingMissingEntry(int IdClockInDetail)
        {
            try
            {
                var result = await _shiftService.TogglingMissingEntry(IdClockInDetail);

                if (!result)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Could not toggle records"));

                return Ok(ApiResponseDto<string>.CreateSuccess("Clock-in/Out Toggling updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetForgotCardEntryDetailsForApproval")]
        public async Task<IActionResult> GetForgotCardEntryDetailsForApproval(DateTime? dateFrom, string? approvalStatus)
        {
            try
            {
                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                var details = await _shiftService.GetForgotCardEntryDetailsForApproval(Convert.ToInt32(employeeId), dateFrom, approvalStatus);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ForgotCardEntryForApprovalDto>>.CreateSuccess(
                        Enumerable.Empty<ForgotCardEntryForApprovalDto>(),
                        "No clock-in/out records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<ForgotCardEntryForApprovalDto>>.CreateSuccess(details, "Clock-in/out details for approval retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

    }
}

