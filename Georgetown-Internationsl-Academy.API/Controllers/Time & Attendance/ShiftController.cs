using Asp.Versioning;
using FluentValidation;
using Georgetown_International_Academy.API.Services.Implementations.TimeAndAttendance;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Georgetown_Internationsl_Academy.API.Services.Interface.Shift;
using Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers.Time___Attendance
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
            _shiftScheduleValidator = shiftScheduleValidator;
            _appproveTimeSheetValidator = appproveTimeSheetValidator;
            _UpdateShortTimeReasonDtoValidator = updateShortTimeReasonDtoValidator;
            _updateClockInOutMissingEntryValidator = updateClockInOutMissingEntryValidator;
            _shiftScheduleService = shiftScheduleService;
            _roleBasedScreenService = roleBasedScreenService;

        }
        #region ShiftDefinitions


        [HttpGet("GetShiftList")]
        public async Task<IActionResult> GetShiftList()
        {
            try
            {
                var shiftList = await _shiftService.GetShiftList();

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
                var validationResult = await _shiftvalidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "A");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var existing = (await _shiftService.GetShiftList())
                    .FirstOrDefault(s => s.ShiftName.Equals(dto.ShiftName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    return Conflict(ApiResponseDto<string>.CreateFailure("Shift already exists."));

                var result = await _shiftService.AddShift(dto);
                if (result == null)
                    return UnprocessableEntity(ApiResponseDto<string>.CreateFailure("Failed to add shift."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Shift added successfully."));
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
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "U");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var duplicate = (await _shiftService.GetShiftList())
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

        #endregion



        #region ClockInClockOutDeatils
        [HttpGet("GetClockInClockOutDetails")]
        public async Task<IActionResult> GetClockInClockOutDetails(string idEmployeeString, DateTime? dateFrom, DateTime? dateTo)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(idEmployeeString))
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID(s) are required."));

            if (!dateFrom.HasValue)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Start date (dateFrom) is required."));

            if (!dateTo.HasValue)
                return BadRequest(ApiResponseDto<string>.CreateFailure("End date (dateTo) is required."));

            try
            {
                var details = await _shiftService.GetClockInClockOutDetailsAsync(idEmployeeString, dateFrom.Value, dateTo.Value);

                if (details == null || !details.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ClockInOutDto>>.CreateSuccess(Enumerable.Empty<ClockInOutDto>(), "No clock-in/out records found."));
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

            foreach (var dto in dtos)
            {
                var validationResult = await _updateClockInOutMissingEntryValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join(" | ", validationResult.Errors.Select(e => $"ID {dto.IdClockDetail}: {e.ErrorMessage}"));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errorMessages}"));
                }
            }

            var success = await _shiftService.UpdateClockInOutMissingEntriesAsync(dtos);
            if (!success)
                return BadRequest(ApiResponseDto<string>.CreateFailure("One or more records could not be updated."));

            return Ok(ApiResponseDto<string>.CreateSuccess("All entries updated successfully."));
        }

        #endregion

        #region Dayttendance
        [HttpGet("GetDayAttendanceDetails")]
        public async Task<IActionResult> GetDayAttendanceDetails(
    DateTime dateFrom , DateTime dateTo , string? idEmployee = null, int? idDepartment = null)
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



        #endregion



    }
}
