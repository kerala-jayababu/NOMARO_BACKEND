using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    //[EnableRateLimiting("FixedWindowPolicy")]
    public class MasterDataController : ControllerBase
    {
        private readonly IValidator<BudgetCodeDto> _budgetvalidator;
        private readonly IValidator<DesignationDto> _designationvalidator;
        private readonly IValidator<DepartmentDto> _deparmentvalidator;
        private readonly IValidator<SalaryHeadDto> _salaryvalidator;
        private readonly IValidator<SystemParameterDto> _systsemValidator;

        private readonly IBudgetCodeServices _budgetCodeServices;
        private readonly IDesignationServices _designationservices;
        private readonly IDepartmentServices _deparmentservices;
        private readonly ISalaryHeadServices _salaryservice;
        private readonly ISystemParameterService _systemParameterService;
        private readonly INotificationConfigService _notificationConfigService;
        private readonly IValidator<NotificationConfigDto> _notificationvalidator;
        private readonly IVacationModeService _vacationModeService;
        private readonly IValidator<VacationModeDto> _vacationModevalidator;
        public MasterDataController(IBudgetCodeServices budgetCodeServices, 
            IDesignationServices designationServices,
            IDepartmentServices deparmentservices,
            ISystemParameterService systemParameterService,
            IValidator<BudgetCodeDto> budgetvalidator,
            IValidator<DesignationDto> designationvalidator,
            IValidator<DepartmentDto> departmentValidator,
            IValidator<SystemParameterDto> systsemValidator,
            INotificationConfigService notificationConfigService, 
            IValidator<NotificationConfigDto> notificationvalidator,
            IVacationModeService vacationModeService, 
            IValidator<VacationModeDto> vacationModevalidator,
            IValidator<SalaryHeadDto> salaryvalidator,            
            ISalaryHeadServices salaryservice)
        {
            _budgetCodeServices = budgetCodeServices;
            _designationservices = designationServices;
            _deparmentservices = deparmentservices;
            _budgetvalidator = budgetvalidator;
            _designationvalidator = designationvalidator;
            _deparmentvalidator = departmentValidator;
            _systsemValidator = systsemValidator;
            _salaryvalidator = salaryvalidator;            
            _salaryservice = salaryservice;
            _systemParameterService = systemParameterService;
            _notificationConfigService = notificationConfigService;
            _notificationvalidator = notificationvalidator;
            _vacationModeService = vacationModeService;
            _vacationModevalidator = vacationModevalidator;
        }

        #region BudgetCodes

        [HttpGet("GetBudgetList")]
        public async Task<IActionResult> GetBudgetList()
        {
            try
            {
                var budgetList = await _budgetCodeServices.GetBudgetList();

                if (!budgetList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No budget codes found."));
                }

                return Ok(ApiResponseDto<IEnumerable<BudgetCodeDto>>.CreateSuccess(budgetList, "Budget list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetBudgetById")]
        public async Task<IActionResult> GetBudgetCodeByID(int Id)
        {
            try
            {
                var budget = await _budgetCodeServices.GetBudgetCodeByID(Id);

                if (budget == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No budget codes found."));
                }

                return Ok(ApiResponseDto<BudgetCodeDto>.CreateSuccess(budget, "Budget  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("AddBudgetCode")]
        public async Task<IActionResult> AddBudgetCode([FromBody] BudgetCodeDto dto)
        {
            var validationResult = await _budgetvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var existingBudgetCode = (await _budgetCodeServices.GetBudgetList())
                .FirstOrDefault(b => b.BudgetCode == dto.BudgetCode  && b.BudgetCodeName == dto.BudgetCodeName);

            if (existingBudgetCode != null)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure("Budget code already exists."));
            }

            try
            {
               var res= await _budgetCodeServices.AddBudgetCode(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create budget code."));
                }
                return Ok(ApiResponseDto<string>.CreateSuccess("Budget code created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UpdateBudgetCode")]
        public async Task<IActionResult> UpdateBudgetCode(BudgetCodeDto dto)
        {
            // Validate the DTO
            var validationResult = await _budgetvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            // Ensure Id is provided and greater than 0
            if (dto.IdBudgetCode == null || dto.IdBudgetCode <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a budget code."));
            }

            try
            {
                // Ensure the budget code exists
                var existingBudgetCode = await _budgetCodeServices.GetBudgetCodeByID(dto.IdBudgetCode.Value);
                if (existingBudgetCode == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Budget code not found."));
                }

                // Check for conflicts with other entries
                var conflictBudgetCode = (await _budgetCodeServices.GetBudgetList())
                    .FirstOrDefault(b => b.BudgetCode == dto.BudgetCode && b.BudgetCodeName == dto.BudgetCodeName);

                if (conflictBudgetCode != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Budget code name conflicts with an existing entry."));
                }

                // Update the budget code in the repository
                var res = await _budgetCodeServices.UpdateBudgetCode(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update budget code."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Budget code updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region Designations

        [HttpGet("GetDesignationList")]
        public async Task<IActionResult> GetDesignationList()
        {
            try
            {
                var designationList = await _designationservices.GetDesignationList();

                if (!designationList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No designations found."));
                }

                return Ok(ApiResponseDto<IEnumerable<DesignationDto>>.CreateSuccess(designationList, "Designation list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetDesignationByID")]
        public async Task<IActionResult> GetDesignationByID(int Id)
        {
            try
            {
                var designation = await _designationservices.GetDesignationByID(Id);

                if (designation == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No designation  found."));
                }

                return Ok(ApiResponseDto<DesignationDto>.CreateSuccess(designation, "designation  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddDesignation")]
        public async Task<IActionResult> AddDesignation([FromBody] DesignationDto dto)
        {
            var validationResult = await _designationvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var existingDesignation = (await _designationservices.GetDesignationList())
                .FirstOrDefault(d => d.DesignationCode == dto.DesignationCode && d.DesignationName == dto.DesignationName);

            if (existingDesignation != null)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure("Designation already exists."));
            }

            try
            {
                var res = await _designationservices.AddDesignation(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create designation."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Designation created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateDesignation")]
        public async Task<IActionResult> UpdateDesignation([FromBody] DesignationDto dto)
        {
            var validationResult = await _designationvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdDesignation == null || dto.IdDesignation <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a designation."));
            }

            try
            {
                var existingDesignation = await _designationservices.GetDesignationByID(dto.IdDesignation.Value);
                if (existingDesignation == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Designation not found."));
                }

                var conflictDesignation = (await _designationservices.GetDesignationList())
                    .FirstOrDefault(d => d.DesignationCode == dto.DesignationCode && d.DesignationName == dto.DesignationName);

                if (conflictDesignation != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Designation name conflicts with an existing entry."));
                }

               var res= await _designationservices.UpdateDesignation(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update designation."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Designation updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion


        #region Departments

        [HttpGet("GetDepartmentList")]
        public async Task<IActionResult> GetDepartmentList()
        {
            try
            {
                var departmentList = await _deparmentservices.GetDepartmentList();

                if (!departmentList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No departments found."));
                }

                return Ok(ApiResponseDto<IEnumerable<DepartmentDto>>.CreateSuccess(departmentList, "Department list retrieved successfully."));
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("GetDepartmentByID")]
        public async Task<IActionResult> GetDepartmentByID(int Id)
        {
            try
            {
                var deparment = await _deparmentservices.GetDepartmentByID(Id);

                if (deparment == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No designation  found."));
                }

                return Ok(ApiResponseDto<DepartmentDto>.CreateSuccess(deparment, "deparment  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddDepartment")]
        public async Task<IActionResult> AddDepartment([FromBody] DepartmentDto dto)
        {
            var validationResult = await _deparmentvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            var existingDepartment = (await _deparmentservices.GetDepartmentList())
                .FirstOrDefault(d => d.DepartmentCode == dto.DepartmentCode && d.DepartmentName == dto.DepartmentName);

            if (existingDepartment != null)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure("Department already exists."));
            }

            try
            {
               var res= await _deparmentservices.AddDepartment(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create deparment."));
                }
                return Ok(ApiResponseDto<string>.CreateSuccess("Department created successfully."));
            }
            catch (Exception ex)
            {
         
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateDepartment")]
        public async Task<IActionResult> UpdateDepartment([FromBody] DepartmentDto dto)
        {
            var validationResult = await _deparmentvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdDepartment == null || dto.IdDepartment <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a department."));
            }

            try
            {
                var existingDepartment = await _deparmentservices.GetDepartmentByID(dto.IdDepartment.Value);
                if (existingDepartment == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Department not found."));
                }

                var conflictDepartment = (await _deparmentservices.GetDepartmentList())
                    .FirstOrDefault(d => d.DepartmentCode == dto.DepartmentCode && d.DepartmentName == dto.DepartmentName);

                if (conflictDepartment != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Department conflicts with an existing entry."));
                }

               var res= await _deparmentservices.UpdateDepartment(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update deparment."));
                }
                return Ok(ApiResponseDto<string>.CreateSuccess("Department updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region SalaryHeads

        [HttpGet("GetSalaryHeadList")]
        public async Task<IActionResult> GetSalaryHeadList()
        {
            try
            {
                var salaryHeadList = await _salaryservice.GetSalaryHeadList();

                if (!salaryHeadList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salary heads found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryHeadDto>>.CreateSuccess(salaryHeadList, "Salary head list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetSalaryHeadByID")]
        public async Task<IActionResult> GetSalaryHeadByID(int Id)
        {
            try
            {
                var salaryHead = await _salaryservice.GetSalaryHeadByID(Id);

                if (salaryHead == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No salaryHead  found."));
                }

                return Ok(ApiResponseDto<SalaryHeadDto>.CreateSuccess(salaryHead, "salaryHead  retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddSalaryHead")]
        public async Task<IActionResult> AddSalaryHead([FromBody] SalaryHeadDto dto)
        {
            var validationResult = await _salaryvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }
            if(dto.IdPercentageSalaryHead > 0)
            {
                var percentageSalaryHeadExists = (await _salaryservice.GetSalaryHeadList())
                .Any(s => s.IdSalaryHead == dto.IdPercentageSalaryHead);

                if (!percentageSalaryHeadExists)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("IdPercentageSalaryHead does not exist."));
                }


            }
            else
            {
                var existingSalaryHead = (await _salaryservice.GetSalaryHeadList())
                .FirstOrDefault(s => s.SalaryHeadCode == dto.SalaryHeadCode && s.SalaryHeadName == dto.SalaryHeadName);

                if (existingSalaryHead != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Salary head already exists."));
                }

            }            

            try
            {
               var res= await _salaryservice.AddSalaryHead(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create salary head."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary head created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateSalaryHead")]
        public async Task<IActionResult> UpdateSalaryHead(SalaryHeadDto dto)
        {
            var validationResult = await _salaryvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdSalaryHead == null || dto.IdSalaryHead <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a salary head."));
            }

            try
            {
                var existingSalaryHead = await _salaryservice.GetSalaryHeadByID(dto.IdSalaryHead.Value);
                if (existingSalaryHead == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Salary head not found."));
                }

                var conflictSalaryHead = (await _salaryservice.GetSalaryHeadList())
                    .FirstOrDefault(s => s.SalaryHeadCode == dto.SalaryHeadCode && s.SalaryHeadName == dto.SalaryHeadName);

                if (conflictSalaryHead != null)
                {
                    return Conflict(ApiResponseDto<string>.CreateFailure("Salary head conflicts with an existing entry."));
                }

              var res=    await _salaryservice.UpdateSalaryHead(dto);
                if (res == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update salary head."));
                }
                return Ok(ApiResponseDto<string>.CreateSuccess("Salary head updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        #endregion

        #region SystemParameters
        [HttpGet("GetSystemParameters")]
        public async Task<IActionResult> GetSystemParameters()
        {
            try
            {
                var parameters = await _systemParameterService.GetAllSystemParameters();

                if (!parameters.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No system parameters found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SystemParameterDto>>.CreateSuccess(parameters, "System parameters retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetSystemParameterById")]
        public async Task<IActionResult> GetSystemParameterById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid System Parameter ID."));
            }

            try
            {
                var parameter = await _systemParameterService.GetSystemParameterById(id);

                if (parameter == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure($"System parameter not found for ID: {id}"));
                }

                return Ok(ApiResponseDto<SystemParameterDto>.CreateSuccess(parameter, "System parameter retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateSystemParameter")]
        public async Task<IActionResult> UpdateSystemParameter([FromBody] SystemParameterDto dto)
        {
          

            // Ensure ID is valid
            if (dto.IdSystemParameter <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid System Parameter ID."));
            }

            try
            {
                var existingParameter = await _systemParameterService.GetSystemParameterById(dto.IdSystemParameter);
                if (existingParameter == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure($"System parameter not found for ID: {dto.IdSystemParameter}"));
                }

                var result = await _systemParameterService.UpdateSystemParameter(dto);
                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update system parameter."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("System parameter updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        #endregion

        #region NotificationConfig
        [HttpGet("GetNotificationConfigList")]
        public async Task<IActionResult> GetNotificationConfigList()
        {
            try
            {
                var notificationConfigList = await _notificationConfigService.GetNotificationConfigList();

                if (!notificationConfigList.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No notification configurations found."));
                }

                return Ok(ApiResponseDto<IEnumerable<NotificationConfigDto>>.CreateSuccess(notificationConfigList, "Notification configuration list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetNotificationConfigByID")]
        public async Task<IActionResult> GetNotificationConfigByID(int id)
        {
            try
            {
                var notificationConfig = await _notificationConfigService.GetNotificationConfigById(id);

                if (notificationConfig == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Notification configuration not found."));
                }

                return Ok(ApiResponseDto<NotificationConfigDto>.CreateSuccess(notificationConfig, "Notification configuration retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddNotificationConfig")]
        public async Task<IActionResult> AddNotificationConfig([FromBody] NotificationConfigDto dto)
        {
           

            var existingNotificationConfig = (await _notificationConfigService.GetNotificationConfigList())
                .FirstOrDefault(n => n.NotificationType == dto.NotificationType);

            if (existingNotificationConfig != null)
            {
                return Conflict(ApiResponseDto<string>.CreateFailure("Notification configuration already exists."));
            }

            try
            {
                var result = await _notificationConfigService.AddNotificationConfig(dto);
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to create notification configuration."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Notification configuration created successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateNotificationConfig")]
        public async Task<IActionResult> UpdateNotificationConfig([FromBody] NotificationConfigDto dto)
        {
            var validationResult = await _notificationvalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            if (dto.IdNotificationConfig <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for updating a notification configuration."));
            }

            try
            {
                var existingNotificationConfig = await _notificationConfigService.GetNotificationConfigById(dto.IdNotificationConfig);
                if (existingNotificationConfig == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Notification configuration not found."));
                }

                var result = await _notificationConfigService.UpdateNotificationConfig(dto);
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update notification configuration."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Notification configuration updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion

        #region VactionMode
        [HttpGet("GetAllVacationModes")]
        public async Task<IActionResult> GetAllVacationModes()
        {
            try
            {
                var vacationModes = await _vacationModeService.GetAllVacationModes();

                if (!vacationModes.Any())
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("No vacation modes found."));
                }

                return Ok(ApiResponseDto<IEnumerable<VacationModeDto>>.CreateSuccess(vacationModes, "Vacation modes retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetVacationModeById")]
        public async Task<IActionResult> GetVacationModeById(int id)
        {
            try
            {
                var vacationMode = await _vacationModeService.GetVacationModeById(id);

                if (vacationMode == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Vacation mode not found."));
                }

                return Ok(ApiResponseDto<VacationModeDto>.CreateSuccess(vacationMode, "Vacation mode retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddVacationMode")]
        public async Task<IActionResult> AddVacationMode([FromBody] VacationModeDto dto)
        {
            var validationResult = await _vacationModevalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _vacationModeService.AddVacationMode(dto);

                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add vacation mode."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Vacation mode added successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("UpdateVacationMode")]
        public async Task<IActionResult> UpdateVacationMode([FromBody] VacationModeDto dto)
        {
            var validationResult = await _vacationModevalidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }

            try
            {
                var result = await _vacationModeService.UpdateVacationMode(dto);

                if (result == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateFailure("Vacation mode not found."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Vacation mode updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        #endregion





    }
}
