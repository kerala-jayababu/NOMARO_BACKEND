using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
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
    public class CommonController : ControllerBase
    {
        private readonly IOptionService _optionService;
        private readonly IApprovalWorkflowService _approveWorkflowService;
        private readonly IRoleBasedScreenService _roleBasedScreenService;

        public CommonController(IOptionService optionService, IApprovalWorkflowService approveWorkflowService,IRoleBasedScreenService roleBasedScreenService)
        {
            _optionService = optionService;
            _approveWorkflowService = approveWorkflowService;
            _roleBasedScreenService = roleBasedScreenService;
        }

        [HttpGet("GetAllOptions")]
        public async Task<IActionResult> GetAllOptions()
        {
            try
            {
                var options = await _optionService.GetAllOptions();

                if (options == null)
                {
                    return Ok(Enumerable.Empty<AllOptionsDto>());
                }

                return Ok(options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetSalaryOptions")]
        public async Task<IActionResult> GetSalaryOptions()
        {
            try
            {
                var options = await _optionService.GetSalaryOptions();

                if (options == null)
                {
                    return Ok(Enumerable.Empty<AllOptionsDto>());
                }

                return Ok(options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetAllSalaryMonths")]
        public async Task<IActionResult> GetAllSalaryMonths()
        {
            var result = await _optionService.GetAllSalaryMonths();
            return Ok(result);
        }

        [HttpGet("GetEmployeeWorkTypes")]
        public async Task<IActionResult> GetEmployeeWorkTypes()
        {
            var result = await _optionService.GetEmployeeWorkTypes();
            return Ok(result);
        }

        [HttpGet("GetAllFiancialyear")]
        public async Task<IActionResult> GetAllFiancialyear()
        {
            var result = await _optionService.GetAllFiancialyear();
            return Ok(result);
        }

        [HttpGet("GetHolidayTypes")]
        public async Task<IActionResult> GetHolidayTypes()
        {
            var result = await _optionService.GetHolidayTypes();
            return Ok(result);
        }

        [HttpGet("GetQualificationTypes")]
        public async Task<IActionResult> GetQualificationTypes()
        {
            var result = await _optionService.GetQualificationTypes();
            return Ok(result);
        }


        [HttpGet("GetEmployeeLatestSalaryStructure")]
        public async Task<IActionResult> GetEmployeeLatestSalaryStructure()
        {
            var result = await _optionService.GetEmployeeLatestSalaryStructure();
            return Ok(result);
        }

        [HttpGet("GetEmployeeNotification")]
        public async Task<IActionResult> GetEmployeeNotification()
        {
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _optionService.GetEmployeeNotification(int.Parse(IdEmployee));
            return Ok(result);
        }


        [HttpPost("UpdateEmployeeNotification")]
        public async Task<IActionResult> UpdateEmployeeNotification(int IdNotification)
        {  

            try
            {
                var result = await _optionService.UpdateEmployeeNotification(IdNotification);
               
                if (result == null)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to update Notification."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Nitification Updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));

            }
        }


        /// <summary>
        /// Handles the approval workflow. Initiates, approves, or rejects a workflow.
        /// </summary>
        [HttpPost("HandleApprovalWorkflow")]
        public async Task<IActionResult> HandleApprovalWorkflow([FromBody] List<ApprovalWorkflowRequestDto> requests)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                              .SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage));

                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }


            var results = new List<string>();
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //int targetScreenId = requests.FirstOrDefault()?.IdPayRollScreen ?? 0;
            //if (targetScreenId == 0)
            //{
            //    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid screen ID."));
            //}

            //var employeePayrollScreen = await _roleBasedScreenService.GetEmployeePermission(int.Parse(IdEmployee),(int)requests.FirstOrDefault().IdPayRollScreen);
            //if (employeePayrollScreen == null || !HasValidPermissions(employeePayrollScreen, (int)requests.FirstOrDefault().IdPayRollScreen, 'A'))
            //{
            //    return BadRequest(ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
            //}

            int count = 1;

            foreach (var request in requests)
            {
                try
                {

                    var result = await _approveWorkflowService.InitiateApprovalWorkflow(
                        request.EntityTablePrimaryKeyID,
                        request.EntityCode,
                        int.Parse(IdEmployee),
                        request.Status,
                        request.LeavePassageAmount,
                        request.RejectReason,
                        count
                    );

                    if (result.Contains("Error"))
                    {
                        results.Add($"Failed for ID {request.EntityTablePrimaryKeyID}: {result}");
                        continue;
                    }
                    count++;
                    results.Add($"Success for ID {request.EntityTablePrimaryKeyID}: {result}");
                }
                catch (ArgumentNullException argEx)
                {
                    results.Add($"ArgumentNullException for ID {request.EntityTablePrimaryKeyID}: {argEx.Message}");
                }
                catch (InvalidOperationException invalidOpEx)
                {
                    results.Add($"InvalidOperationException for ID {request.EntityTablePrimaryKeyID}: {invalidOpEx.Message}");
                }
                catch (FormatException formatEx)
                {
                    results.Add($"FormatException for ID {request.EntityTablePrimaryKeyID}: {formatEx.Message}");
                }
                catch (Exception ex)
                {
                    results.Add($"Exception for ID {request.EntityTablePrimaryKeyID}: {ex.Message}");
                }               
            }

            if (results.Any(r => r.StartsWith("Failed") || r.StartsWith("Exception")))
            {
                // Return a list of results as a failure message
                return BadRequest(ApiResponseDto<List<string>>.CreateFailure(results, "Some approval workflows failed."));
            }

            return Ok(ApiResponseDto<List<string>>.CreateSuccess(results, "All approval workflows processed successfully."));
        }
        private bool HasValidPermissions(EmployeePermissions screens, int targetScreenId, char requiredPermission)
        {


            if (screens.IdPayrollScreen == targetScreenId && !string.IsNullOrEmpty(screens.Permission) && screens.Permission.Contains(requiredPermission))
            {

                return true;
            }


            return false; 
        }


        [HttpGet("GetCountries")]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var data = await _optionService.GetCountries();

                return Ok(ApiResponseDto<IEnumerable<CountryDto>>
                    .CreateSuccess(data, "Countries retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }
        [HttpGet("GetDocumentTypes")]
        public async Task<IActionResult> GetDocumentTypes()
        {
            try
            {
                var data = await _optionService.GetDocumentTypes();

                if (data == null || !data.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<DocumentTypeDto>>
                        .CreateSuccess(Enumerable.Empty<DocumentTypeDto>(), "No document types found."));
                }

                return Ok(ApiResponseDto<IEnumerable<DocumentTypeDto>>
                    .CreateSuccess(data, "Document types retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


    }
}
