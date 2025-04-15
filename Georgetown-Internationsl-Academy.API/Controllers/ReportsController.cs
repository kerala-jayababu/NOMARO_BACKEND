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
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportServices _reportService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        public ReportsController(IReportServices reportService, IConfiguration configuration, IRoleBasedScreenService roleBasedService)
        {
            _reportService = reportService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
        }
        [HttpGet("GetReportMasters")]
        public async Task<IActionResult> GetReportMasters()
        {
            try
            {
                var ReportsList = await _reportService.GetReportMasters();

                if (!ReportsList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ReportsMasterDto>>.CreateSuccess(Enumerable.Empty<ReportsMasterDto>(), "No Reports available."));
                }

                return Ok(ApiResponseDto<IEnumerable<ReportsMasterDto>>.CreateSuccess(ReportsList, "Reports list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("GetReportConditionById")]
        public async Task<IActionResult> GetReportConditionById(int id)
        {
            try
            {
                var ReportConditionsList = await _reportService.GetReportConditionById(id);

                if (!ReportConditionsList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<ReportConditionsDto>>.CreateSuccess(Enumerable.Empty<ReportConditionsDto>(), "No Reports available."));
                }

                return Ok(ApiResponseDto<IEnumerable<ReportConditionsDto>>.CreateSuccess(ReportConditionsList, "Reports list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("ExecuteStoredProcedure")]
        public async Task<IActionResult> ExecuteStoredProcedure([FromBody] StoredProcedureDto request)
        {
            var result = await _reportService.ExecuteStoredProcedureAsync(request);
            return Ok(result);
        }


        [HttpPost("GetReportsTableValue")]
        public async Task<IActionResult> GetReportsTableValue(string tableName)
        {
            var result = await _reportService.GetReportsTableValue(tableName);
            return Ok(result);
        }





        /*
         * {
              "StoredProcedureName": "sp_GetPayrollSummary",
              "Parameters": {
                "@Month": "March",
                "@Year": 2025
              }
            }*/
    }
}
