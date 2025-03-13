using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class BankController : ControllerBase
    {
        private readonly IBankServices _bankservice;
    
        public BankController(IBankServices bankservice)
        {
            _bankservice = bankservice;
            
        }

        [HttpGet("GetBanksList")]
        public async Task<IActionResult> GetBanksList()
        {
            try
            {
                var banksList = await _bankservice.GetBanksList();

                if (!banksList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<BankDto>>.CreateSuccess(new List<BankDto>(), "No banks available."));
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
                    return Ok(ApiResponseDto<IEnumerable<BankBranchesDto>>.CreateSuccess(new List<BankBranchesDto>(), "No branches available for the selected bank."));
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

            try
            {
                var isSuccess = await _bankservice.AddOrUpdateBranchesOfBank(bankBranchesDtoList);

                if (!isSuccess)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to add or update bank branches. Please try again."));
                }

                return Ok(ApiResponseDto<bool>.CreateSuccess(true, "Bank branches added/updated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }




    }
}
