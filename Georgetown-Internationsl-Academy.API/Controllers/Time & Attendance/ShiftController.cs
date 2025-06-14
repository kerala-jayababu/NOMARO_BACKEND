using Asp.Versioning;
using FluentValidation;
using Georgetown_International_Academy.API.Services.Implementations.TimeAndAttendance;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
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
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _shiftService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedScreenService;
        private readonly IShiftScheduleService _shiftScheduleService;
        private readonly IValidator<ShiftDto> _shiftvalidator;
        private readonly IValidator<ShiftScheduleDto> _shiftScheduleValidator;

        public ShiftController(IShiftService shiftService, IConfiguration configuration, 
            IRoleBasedScreenService roleBasedScreenService,
            IShiftScheduleService shiftScheduleService,
            IValidator<ShiftDto>  shiftValidator,
             IValidator<ShiftScheduleDto> shiftScheduleValidator)
        {
            _shiftService = shiftService;
            _configuration = configuration;
            _shiftvalidator = shiftValidator;
            _shiftScheduleValidator = shiftScheduleValidator;
            _shiftScheduleService=shiftScheduleService;
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
        public async Task<IActionResult> GetShiftScheduleList()
        {
            try
            {
                var list = await _shiftScheduleService.GetAllShiftSchedulesAsync();
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

        [HttpGet("GetShiftScheduleById")]
        public async Task<IActionResult> GetShiftScheduleById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid shift schedule ID."));
            }

            try
            {
                var shift = await _shiftScheduleService.GetShiftScheduleByIdAsync(id);
                if (shift == null)
                {
                    return Ok(ApiResponseDto<ShiftScheduleDto>.CreateSuccess(null, "Shift schedule not found."));
                }

                return Ok(ApiResponseDto<ShiftScheduleDto>.CreateSuccess(shift, "Shift schedule retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddShiftSchedule")]
        public async Task<IActionResult> AddShiftSchedule( ShiftScheduleDto dto)
        {
            try
            {
                var validation = await _shiftScheduleValidator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));


                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "A");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));
                var existing = (await _shiftScheduleService.GetAllShiftSchedulesAsync())
                                .FirstOrDefault(s =>
                                    s.IdShift == dto.IdShift &&
                                    s.StartTime == dto.StartTime &&
                                    s.EndTime == dto.EndTime);

                if (existing != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Shift schedule with same shift, start time, and end time already exists."));
                }
                var result = await _shiftScheduleService.AddShiftScheduleAsync(dto);
                if (result == null)
                {
                    return UnprocessableEntity(ApiResponseDto<string>.CreateFailure("Failed to add shift schedule."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Shift schedule added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateShiftSchedule")]
        public async Task<IActionResult> UpdateShiftSchedule(ShiftScheduleDto dto)
        {
            try
            {
                if (dto == null || dto.IdShiftSchedule == null || dto.IdShiftSchedule <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid shift schedule ID."));
                }

                var validation = await _shiftScheduleValidator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var employeeId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employeeId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
                // var hasPermission = await _roleBasedScreenService.CheckEmployeePermission(int.Parse(employeeId), _configuration["ScreenCodes:Shifts"], "U");
                // if (!hasPermission)
                //     return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));
                var duplicate = (await _shiftScheduleService.GetAllShiftSchedulesAsync())
                    .FirstOrDefault(s =>
                        s.IdShift == dto.IdShift &&
                        s.StartTime == dto.StartTime &&
                        s.EndTime == dto.EndTime &&
                        s.IdShiftSchedule != dto.IdShiftSchedule);

                if (duplicate != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Another shift schedule with same shift, start time, and end time already exists."));
                }
                var result = await _shiftScheduleService.UpdateShiftScheduleAsync(dto);
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update shift schedule."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Shift schedule updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

    }
}
