using Asp.Versioning;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
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

        public CommonController(IOptionService optionService, IApprovalWorkflowService approveWorkflowService)
        {
            _optionService = optionService;
            _approveWorkflowService = approveWorkflowService;
        }

        [HttpGet("GetAllOptions")]
        public async Task<IActionResult> GetAllOptions()
        {
            try
            {
                var options = await _optionService.GetAllOptions();

                if (options == null)
                {
                    return NotFound("No options found.");
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


        [HttpGet("GetEmployeeLatestSalaryStructure")]
        public async Task<IActionResult> GetEmployeeLatestSalaryStructure()
        {
            var result = await _optionService.GetEmployeeLatestSalaryStructure();
            return Ok(result);
        }


        /// <summary>
        /// Handles the approval workflow. Initiates, approves, or rejects a workflow.
        /// </summary>
        [HttpPost("HandleApprovalWorkflow")]
        public async Task<IActionResult> HandleApprovalWorkflow([FromBody] ApprovalWorkflowRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                                 .SelectMany(v => v.Errors)
                                                 .Select(e => e.ErrorMessage));

                return BadRequest(ApiResponseDto<string>.CreateFailure($"Validation failed: {errors}"));
            }


            try
            {
                var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;


                var result = await _approveWorkflowService.InitiateApprovalWorkflow(
                    request.EntityTablePrimaryKeyID,
                    request.EntityCode,
                    int.Parse(IdEmployee),
                    request.Status,
                    request.RejectReason
                );

                if (result.Contains("Error"))
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure(result));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess(result, "Approval workflow processed successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }




    }
}
