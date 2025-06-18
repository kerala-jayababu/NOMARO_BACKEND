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
using System.Numerics;
using System.Security.Claims;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]

    public class ReportsController : ControllerBase
    {
        private readonly IReportServices _reportService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly ISystemParameterService _systemParameterService;
        private readonly IOptionService _optionService;
        public ReportsController(IReportServices reportService, IConfiguration configuration,IOptionService optionService,  IRoleBasedScreenService roleBasedService, ISystemParameterService systemParameterService)
        {
            _reportService = reportService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _optionService = optionService;
            _systemParameterService = systemParameterService;
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
        public async Task<IActionResult> GetReportsTableValue(string tableName, string valueColumn, string displayColumn)
        {
            var result = await _reportService.GetReportsTableValue(tableName, valueColumn, displayColumn);
            return Ok(result);
        }


        [HttpPost("GetReportColumnsById")]
        public async Task<IActionResult> GetReportColumnsById(int IdReport)
        {
            var result = await _reportService.GetReportColumnsById(IdReport);
            return Ok(result);
        }

        [HttpPost("GenerateIncomeTax")]
        public async Task<IActionResult> GenerateIncomeTaxReport(int payrollId)
        {
            var systemParameters = await _systemParameterService.GetAllSystemParameters();
            var salaryMonths = await _optionService.GetAllSalaryMonths();

            var salaryMonthText = salaryMonths
                .Where(x => x.IdSalaryMonth == payrollId)
                .Select(x => x.SalaryMonthText)
                .FirstOrDefault();

            var taxOfficeAddress = systemParameters
                .Where(x => x.ParameterName == "TaxOfficeAddress")
                .Select(c => c.ParameterValue)
                .FirstOrDefault();

            var tinNumber = systemParameters
                .Where(x => x.ParameterName == "TINNumber")
                .Select(c => c.ParameterValue)
                .FirstOrDefault();

            var taxAuthorizedPersonName = systemParameters
                .Where(x => x.ParameterName == "TaxAuthorizedPersonName")
                .Select(c => c.ParameterValue)
                .FirstOrDefault();

            var taxAuthorizedSignatureImage = systemParameters
                .Where(x => x.ParameterName == "CompanySeal")
                .Select(c => c.ParameterBinaryValue)
                .FirstOrDefault();

            var company = new CompanyDetails
            {
                CompanyName = null,
                Address = null,
                RegNumber = null,
                TINNumber = tinNumber,
                TaxAuthorizedPersonName = taxAuthorizedPersonName,
                TaxOfficeAddress = taxOfficeAddress,
                SignatureImage = taxAuthorizedSignatureImage != null
                    ? taxAuthorizedSignatureImage
                    : null
            };

            var pdfBytes = await _reportService.GenerateIncomeTaxReportAsync(payrollId, company, salaryMonthText);
            var fileName = $"IncomeTax_{salaryMonthText.Replace(" ", "_")}.pdf";

            //var response = new PdfFileResponseDto
            //{
            //    FileName = fileName,
            //    FileBytes = Convert.ToBase64String(pdfBytes)
            //};

            return File(pdfBytes, "application/pdf", fileName);
        }


        [HttpPost("generate-nis")]
        public async Task<IActionResult> GenerateNISContributionReport(int payrollId,string ageGroup)
        {

            var systemParameters = await _systemParameterService.GetAllSystemParameters();
            var CompanyName = systemParameters
              .Where(x => x.ParameterName == "CompanyName")
              .Select(c => c.ParameterValue)
              .FirstOrDefault();
            var CompanyAddress = systemParameters
              .Where(x => x.ParameterName == "CompanyAddress")
              .Select(c => c.ParameterValue)
              .FirstOrDefault();
            var RegistrationNumber = systemParameters
              .Where(x => x.ParameterName == "RegistrationNumber")
              .Select(c => c.ParameterValue)
              .FirstOrDefault();
            
            var company = new CompanyDetails
            {
                CompanyName = CompanyName,
                Address = CompanyAddress,
                RegNumber = RegistrationNumber
            };
            var salaryMonths = await _optionService.GetAllSalaryMonths();

            var salaryMonthText = salaryMonths
                .Where(x => x.IdSalaryMonth == payrollId)
                .Select(x => x.SalaryMonthText)
                .FirstOrDefault();


            string salaryMonth = salaryMonthText; // or retrieve dynamically

            var pdfBytes = await _reportService.GenerateNISReportAsync(payrollId, ageGroup, company, salaryMonth);

            var fileName = $"NIS_Report_{salaryMonth.Replace(" ", "_")}_{ageGroup}.pdf";

            var response = new PdfFileResponseDto
            {
                FileName = fileName,
                FileBytes = Convert.ToBase64String(pdfBytes)
            };

            return File(pdfBytes, "application/pdf", fileName);
        }



    }
}
