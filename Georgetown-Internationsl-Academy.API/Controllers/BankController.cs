using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class BankController : ControllerBase
    {
        private readonly IBankServices _bankservice;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public BankController(IBankServices bankservice, IConfiguration configuration, IRoleBasedScreenService roleBasedService)
        {
            _bankservice = bankservice;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }

        [HttpGet("GetBanksList")]
        public async Task<IActionResult> GetBanksList()
        {
            try
            {
                var banksList = await _bankservice.GetBanksList();

                if (!banksList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<BankDto>>.CreateSuccess(Enumerable.Empty<BankDto>(), "No banks available."));
                }

                return Ok(ApiResponseDto<IEnumerable<BankDto>>.CreateSuccess(banksList, "Bank list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetBranchesOfBank")]
        public async Task<IActionResult> GetBranchesOfBank(int idBank)
        {
            try
            {
                if (idBank <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid bank ID. Please provide a valid bank ID."));
                }

                var branchesList = await _bankservice.GetBranchesOfBank(idBank);

                if (!branchesList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<BankBranchesDto>>.CreateSuccess(Enumerable.Empty<BankBranchesDto>(), "No branches available for the selected bank."));
                }
                return Ok(ApiResponseDto<IEnumerable<BankBranchesDto>>.CreateSuccess(branchesList, "Branch list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("AddOrUpdateBranchesOfBank")]
        public async Task<IActionResult> AddOrUpdateBranchesOfBank( List<BankBranchesDto> bankBranchesDtoList)
        {
            if (bankBranchesDtoList == null || !bankBranchesDtoList.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid input. Please provide a valid list of bank branches."));
            }
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            try
            {
                var screenCode = _configuration["ScreenCodes:BankBranches"];
                var actionType = "A";

                // Check permission
                var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

                if (!hasPermission)
                {
                    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

                }

                var isSuccess = await _bankservice.AddOrUpdateBranchesOfBank(bankBranchesDtoList);

                if (!isSuccess)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add or update bank branches. Please try again."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Bank branches added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }




    }
}
