using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeServices _employeeservice;
        private readonly IValidator<List<EmployeeBankAccountDtoList>> _employeeBankAccountvalidator;
        private readonly IValidator<List<EmployeeOvertimeConfigDtoList>> _employeeOverTimevalidator;
        private readonly IValidator<List<EmployeeActionPostDto>> _employeeActionValidator;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IValidator<EmployeeEntityDto> _employeeEntityvalidator;
        private readonly IValidator<List<EmployeeExperienceDto>> _employeeExperienceValidator;
        public EmployeeController(IEmployeeServices employeeservice, IValidator<List<EmployeeBankAccountDtoList>> employeeBankAccountvalidator,
    IConfiguration configuration, IRoleBasedScreenService roleBasedService, IValidator<List<EmployeeOvertimeConfigDtoList>> employeeOverTimevalidator,
    IValidator<EmployeeEntityDto> employeeEntityvalidator, IValidator<List<EmployeeActionPostDto>> employeeActionValidator, IValidator<List<EmployeeExperienceDto>> employeeExperienceValidator)
        {
            _employeeservice = employeeservice;
            _employeeBankAccountvalidator = employeeBankAccountvalidator;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _employeeOverTimevalidator = employeeOverTimevalidator;
            _employeeEntityvalidator = employeeEntityvalidator;
            _employeeActionValidator = employeeActionValidator;
            _employeeExperienceValidator = employeeExperienceValidator;
        }


        [HttpGet("GetEmployeeList")]
        public async Task<IActionResult> GetEmployeeList(string? searchText, DateTime? startDate)
        {
            try
            {
                var employeeList = await _employeeservice.GetEmployeeList(searchText, startDate);

                if (employeeList == null || !employeeList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDto>>.CreateSuccess(Enumerable.Empty<EmployeeProfileDto>(), "No employees found."));
                }

                //foreach (var employee in employeeList)
                //{
                //    string dbPath = employee.EmployeePhotoFilePath?.Trim();


                //    if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
                //    {
                //        employee.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
                //    }

                //    if (!string.IsNullOrEmpty(employee.ChildCountDocumentFilePath) && System.IO.File.Exists(employee.ChildCountDocumentFilePath))
                //    {
                //        employee.AttachmentBlobForchildcount = await System.IO.File.ReadAllBytesAsync(employee.ChildCountDocumentFilePath);
                //        employee.ChildCountDocumentFilePath = Path.GetFileName(employee.ChildCountDocumentFilePath); 

                //    }
                //}
                employeeList.First().ActiveEmployeeCount = employeeList.Count(e => e.CurrentStatus == "Working");

                return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDto>>.CreateSuccess(employeeList, "Employee list retrieved successfully."));
            }
            catch (Exception ex)
            {

                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("EmployeeWithoutSalaryApprovalDto")]
        public async Task<IActionResult> EmployeeWithoutSalaryApprovalDto()
        {
            try
            {
                var employeeList = await _employeeservice.GetEmployeeStatusListAsync();

                if (employeeList == null || !employeeList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeWithoutSalaryApprovalDto>>.CreateSuccess(Enumerable.Empty<EmployeeWithoutSalaryApprovalDto>(), "No employee status records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeWithoutSalaryApprovalDto>>.CreateSuccess(employeeList, "Employee status list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeeDetailsByID")]
        public async Task<IActionResult> GetEmployeeDetailsByID(int Id)
        {
            try
            {
                var employeeDetails = await _employeeservice.GetEmployeeDetailsByID(Id);


                if (employeeDetails == null)
                {
                    return Ok(ApiResponseDto<EmployeeDetailsDto>.CreateSuccess(null, "No employee found with the provided ID."));
                }

                string dbPath = employeeDetails.EmployeePhotoFilePath?.Trim();


                if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
                {
                    employeeDetails.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);
                }
                if (!string.IsNullOrEmpty(employeeDetails.ChildCountDocumentFilePath) && System.IO.File.Exists(employeeDetails.ChildCountDocumentFilePath))
                {
                    employeeDetails.AttachmentBlobForchildcount = await System.IO.File.ReadAllBytesAsync(employeeDetails.ChildCountDocumentFilePath);
                    employeeDetails.ChildCountDocumentFilePath = Path.GetFileName(employeeDetails.ChildCountDocumentFilePath);

                }

                //if (!string.IsNullOrEmpty(employeeDetails.EmployeePhotoFilePath) && System.IO.File.Exists(employeeDetails.EmployeePhotoFilePath))
                //    {
                //    employeeDetails.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(employeeDetails.EmployeePhotoFilePath);
                //   }

                return Ok(ApiResponseDto<EmployeeDetailsDto>.CreateSuccess(employeeDetails, "Employee Details  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetEmployeeProfileByID")]
        public async Task<IActionResult> GetEmployeeProfileByID(int Id)
        {
            try
            {
                var employeeProfile = await _employeeservice.GetEmployeeProfileByID(Id);

                if (employeeProfile == null || !employeeProfile.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDetailsDto>>.CreateSuccess(Enumerable.Empty<EmployeeProfileDetailsDto>(), "No employee bank account details found."));
                }

                foreach (var employee in employeeProfile)
                {

                    string dbPath = employee.EmployeePhotoFilePath?.Trim();


                    if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
                    {
                        employee.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(dbPath);

                    }

                    //if (!string.IsNullOrEmpty(employee.EmployeePhotoFilePath) && System.IO.File.Exists(employee.EmployeePhotoFilePath))
                    //{
                    //    employee.AttachmentBlob = await System.IO.File.ReadAllBytesAsync(employee.EmployeePhotoFilePath);
                    //}

                }
                return Ok(ApiResponseDto<IEnumerable<EmployeeProfileDetailsDto>>.CreateSuccess(employeeProfile, "Employee profile details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeBankAccountsByID")]
        public async Task<IActionResult> GetEmployeeBankAccountsByID(int Id)
        {
            try
            {
                var employeeBankAccounts = await _employeeservice.GetEmployeeBankAccountsByID(Id);

                if (employeeBankAccounts == null || !employeeBankAccounts.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeBankAccountDto>>.CreateSuccess(Enumerable.Empty<EmployeeBankAccountDto>(), "No employee bank account details found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeBankAccountDto>>.CreateSuccess(employeeBankAccounts, "Employee bank account details retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeeOvertimeConfigsByID")]
        public async Task<IActionResult> GetEmployeeOvertimeConfigsByID(int employeeId)
        {
            try
            {
                var overtimeConfigs = await _employeeservice.GetEmployeeOvertimeConfigsByID(employeeId);

                if (overtimeConfigs == null || !overtimeConfigs.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeOvertimeConfigDto>>.CreateSuccess(Enumerable.Empty<EmployeeOvertimeConfigDto>(), "No overtime configurations found for the employee."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeOvertimeConfigDto>>.CreateSuccess(overtimeConfigs, "Overtime configurations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpPost("UpdateEmployeeDetails")]
        public async Task<IActionResult> UpdateEmployeeDetails([FromForm] UpdateEmployeeDto dto)
        {
            if (dto.EmployeeId <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Employee ID. Employee ID must be greater than 0."));
            }
          

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }



            try
            {
                var updateResult = await _employeeservice.UpdateEmployeeDetails(dto);
                if (!updateResult)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update employee."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }

        }

        [HttpPost("DeleteEmployeeAttachment")]
        public async Task<IActionResult> DeleteEmployeeAttachment(int idEmployee)
        {
            try
            {
                var updateResult = await _employeeservice.DeleteEmployeeAttachment(idEmployee);
                if (!updateResult)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to delte attachement."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee attachment deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }

        }

            [HttpPost("ManageEmployeeBankAccounts")]
        public async Task<IActionResult> ManageEmployeeBankAccounts([FromBody] List<EmployeeBankAccountDtoList> bankAccounts)
        {
            if (bankAccounts == null || !bankAccounts.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Bank account list cannot be empty."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var result = await _employeeservice.ManageEmployeeBankAccounts(bankAccounts);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage employee bank accounts."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee bank accounts managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ManageEmployeeOvertimeConfigs")]
        public async Task<IActionResult> ManageEmployeeOvertimeConfigs([FromBody] List<EmployeeOvertimeConfigDtoList> overtimeConfigs)
        {
            if (overtimeConfigs == null || !overtimeConfigs.Any())
            {
                // Return 200 OK instead of BadRequest
                return Ok(ApiResponseDto<string>.CreateSuccess("No overtime configs provided. Nothing to update."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:EmployeeProfile"];
            var actionType = "U";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }

            try
            {
                var result = await _employeeservice.ManageEmployeeOvertimeConfigs(overtimeConfigs);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to manage employee overtime configs."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee overtime configs managed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetEmployeesByHierarchy")]
        public async Task<IActionResult> GetEmployeesByHierarchy(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Employee ID."));
                }

                var result = await _employeeservice.GetEmployeesByHierarchy(employeeId);

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeHierarchyDto>>.CreateSuccess(Enumerable.Empty<EmployeeHierarchyDto>(), "No employees found for the given hierarchy."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeHierarchyDto>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("AddEmployee")]
        public async Task<IActionResult> AddEmployee([FromForm] EmployeeEntityDto dto)
        {
            try
            {
                var validationResult = await _employeeEntityvalidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
                }

                var result = await _employeeservice.AddEmployee(dto);
                return Ok(ApiResponseDto<string>.CreateSuccess("Employee added successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromForm] EmployeeEntityDto dto)
        {
            try
            {
                var validation = await _employeeEntityvalidator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return BadRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

                var result = await _employeeservice.UpdateEmployee(id, dto);
                if (result == null)
                    return NotFound(new { message = "Employee not found or update failed." });
                return Ok(ApiResponseDto<string>.CreateSuccess("Employee updated successfully."));
             
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure(ex.Message));
            }           
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));

            }
        }

        [HttpGet("GetEmployeeById")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid Employee ID." });
            }

            try
            {
                var employee = await _employeeservice.GetEmployeeById(id);

                if (employee == null)
                {
                    return NotFound(new { message = "Employee not found." });
                }

                return Ok(new
                {
                    message = "Employee retrieved successfully.",
                    data = employee
                });
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpGet("GetAssetAssignments")]
        public async Task<IActionResult> GetAssetAssignments(int? idEmployee, int? idAsset)
        {
            var data = await _employeeservice.GetAssetAssignments(idEmployee, idAsset);

            return Ok(ApiResponseDto<IEnumerable<AssetAssignmentFullDto>>
                .CreateSuccess(data, "Asset assignments retrieved successfully."));
        }

        [HttpPost("AssignAsset")]
        public async Task<IActionResult> AssignAsset(AssetAssignmentDto dto)
        {
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            try
            {
                var screenCode = _configuration["ScreenCodes:AssetAssignments"];
                var actionType = "A";

                var hasPermission = await _roleBasedService
                    .CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                    return StatusCode(403,
                        ApiResponseDto<string>.CreateFailure("Permission denied."));

                var success = await _employeeservice.AssignAsset(dto);

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Asset assigned successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure(ex.Message));
            }
        }

        [HttpPost("UnassignAsset")]
        public async Task<IActionResult> UnassignAsset(int idAsset, int idEmployee)
        {
            try
            {
                var success = await _employeeservice.UnassignAsset(idAsset, idEmployee);

                return Ok(ApiResponseDto<string>
                    .CreateSuccess("Asset unassigned successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    ApiResponseDto<string>.CreateFailure(ex.Message));
            }
        }

        [HttpGet("GetEmployeeQualifications")]
        public async Task<IActionResult> GetEmployeeQualifications(int idEmployee, int? idEmployeeQualification)
        {
            try
            {
                if (idEmployee <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdEmployee is mandatory."));

                var data = await _employeeservice.GetEmployeeQualifications(idEmployee, idEmployeeQualification);

                if (data == null || !data.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeQualificationDto>>
                        .CreateSuccess(Enumerable.Empty<EmployeeQualificationDto>(), "No qualifications found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeQualificationDto>>
                    .CreateSuccess(data, "Qualifications retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpDelete("DeleteEmployeeQualification")]
        public async Task<IActionResult> DeleteEmployeeQualification(int idEmployeeQualification)
        {
            try
            {
                if (idEmployeeQualification <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdEmployeeQualification is required."));

                // ✅ Get logged-in employee id
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:EmployeeQualifications"];

                if (!await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D"))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var result = await _employeeservice.DeleteEmployeeQualification(idEmployeeQualification);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to delete employee qualification."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee qualification deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("AddOrUpdateEmployeeQualifications")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddOrUpdateEmployeeQualifications([FromForm] List<EmployeeQualificationDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Qualification list cannot be empty."));

                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeQualifications"];

                if (!await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A"))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var result = await _employeeservice.AddOrUpdateEmployeeQualifications(dtos, loggedInEmployeeId);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save employee qualifications."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee qualifications saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpPost("AddOrUpdateEmployeeExperiences")]
        public async Task<IActionResult> AddOrUpdateEmployeeExperiences([FromForm] List<EmployeeExperienceDto> dtos)
        {
            try
            {
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
                var validationResult = await _employeeExperienceValidator.ValidateAsync(dtos);
                if (!validationResult.IsValid)
                    return BadRequest(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                int loggedInEmployeeId = int.Parse(userId);

                var screenCode = _configuration["ScreenCodes:EmployeeExperiences"];

                if (!await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "A"))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var result = await _employeeservice.AddOrUpdateEmployeeExperiences(dtos, loggedInEmployeeId);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to save employee experiences."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee experiences saved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetEmployeeActions")]
        public async Task<IActionResult> GetEmployeeActions(string? searchText, string? actionType, string? dateFrom)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dateFrom))
                    return BadRequest(ApiResponseDto<string>.CreateFailure("DateFrom is mandatory."));
                if (!DateTime.TryParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture,
               DateTimeStyles.None, out DateTime parsedDate))
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid DateFrom format. Use yyyy-MM-dd."));
                }
                var actions = await _employeeservice.GetEmployeeActions(searchText, actionType, parsedDate);

                if (actions == null || !actions.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeActionDto>>
                        .CreateSuccess(Enumerable.Empty<EmployeeActionDto>(), "No employee actions found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeActionDto>>
                    .CreateSuccess(actions, "Employee actions retrieved successfully."));
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

        [HttpPost("PostEmployeeActions")]
        public async Task<IActionResult> PostEmployeeActions([FromBody] List<EmployeeActionPostDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee action list cannot be empty."));

            // ✅ Get logged-in employee id from token
            var loggedInEmployeeIdStr = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(loggedInEmployeeIdStr))
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

            int loggedInEmployeeId = int.Parse(loggedInEmployeeIdStr);
           

            // ✅ Fluent Validation
            var validationResult = await _employeeActionValidator.ValidateAsync(dtoList);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                var errorMessage = string.Join(" | ", errors);
                return BadRequest(ApiResponseDto<string>.CreateFailure(errorMessage));
            }

            try
            {
                var ids = await _employeeservice.PostEmployeeActions(dtoList, loggedInEmployeeId);

                return Ok(ApiResponseDto<List<int>>.CreateSuccess(ids, "Employee actions saved successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponseDto<string>.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpGet("GetEmployeeExperiences")]
        public async Task<IActionResult> GetEmployeeExperiences(int idEmployee, int? idEmployeeExperience)
        {
            try
            {
                if (idEmployee <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdEmployee is mandatory."));

                var data = await _employeeservice.GetEmployeeExperiences(idEmployee, idEmployeeExperience);

                if (data == null || !data.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<EmployeeExperienceGetDto>>
                        .CreateSuccess(Enumerable.Empty<EmployeeExperienceGetDto>(), "No employee experiences found."));
                }

                return Ok(ApiResponseDto<IEnumerable<EmployeeExperienceGetDto>>
                    .CreateSuccess(data, "Employee experiences retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpDelete("DeleteEmployeeExperience")]
        public async Task<IActionResult> DeleteEmployeeExperience(int idEmployeeExperience)
        {
            try
            {
                if (idEmployeeExperience <= 0)
                    return BadRequest(ApiResponseDto<string>.CreateFailure("IdEmployeeExperience is required."));

                // ✅ Get logged-in employee id
                var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));

                int loggedInEmployeeId = int.Parse(userId);

                // ✅ Permission check
                var screenCode = _configuration["ScreenCodes:EmployeeExperiences"];
                if (!await _roleBasedService.CheckEmployeePermission(loggedInEmployeeId, screenCode, "D"))
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have permission."));

                var result = await _employeeservice.DeleteEmployeeExperience(idEmployeeExperience);

                if (!result)
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to delete employee experience."));

                return Ok(ApiResponseDto<string>.CreateSuccess("Employee experience deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



    }
}
