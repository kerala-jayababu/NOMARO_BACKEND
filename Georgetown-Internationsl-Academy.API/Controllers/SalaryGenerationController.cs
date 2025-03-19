using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Security.Claims;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class SalaryGenerationController : ControllerBase
    {
        private readonly ISalaryHeadServices _salaryservice;
        private readonly ISalaryGenerationService _salaryService;
        private readonly ISalaryHeadServices _salaryHeadService;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedScreenService _roleBasedService;


        public SalaryGenerationController(ISalaryGenerationService salaryService, IConfiguration configuration, ISalaryHeadServices salaryservice, IRoleBasedScreenService roleBasedService, ISalaryHeadServices salaryHeadService)
        {
            _salaryService = salaryService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _salaryHeadService = salaryHeadService;
            _salaryservice = salaryservice;
        }

        /// <summary>
        /// Gets the salary list based on filters.
        /// </summary>
        /// <param name="idSalaryMonth">Salary month ID (Optional)</param>
        /// <param name="searchText">Search text for Employee Code, Name, Department, Designation (Optional)</param>
        /// <param name="dropdownFilter">Status filter: All, Submitted, Approved, Draft Generated, Not Generated (Optional)</param>
        /// <param name="idDepartment">Filter by Department ID (Optional)</param>
        /// <param name="idDesignation">Filter by Designation ID (Optional)</param>
        /// <returns>List of salary configurations</returns>
        [HttpGet("GetSalaryList")]
        public async Task<IActionResult> GetSalaryList(
            [FromQuery] int? idSalaryMonth = null,          
            [FromQuery] string? dropdownFilter = null,
            [FromQuery] int? idDepartment = null,
            [FromQuery] int? idDesignation = null)
        {
            try
            {
                if (idSalaryMonth == null || idSalaryMonth <= 0)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary genetation list."));
                }

                IEnumerable<SalaryGenerationDto>? salaryList = await _salaryService.GetSalaryConfigs(idSalaryMonth, dropdownFilter, idDepartment, idDesignation);

                if (salaryList == null || !salaryList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDto>>.CreateSuccess(Enumerable.Empty<SalaryGenerationDto>(), "No salary records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDto>>.CreateSuccess(salaryList, "Salary list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("GenerateSalaryDraft")]
        public async Task<IActionResult> GenerateSalaryDraft([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth == null || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary genetation list."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idEmployee))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("User not authenticated."));
                }

                var result = await _salaryService.GenerateSalaryDraft(employeeIds, idSalaryMonth, int.Parse(idEmployee));

                if (!result.Any())
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to generate draft salary."));
                }

                return Ok(ApiResponseDto<IEnumerable<dynamic>>.CreateSuccess(result, "Draft Salary generated successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("SubmitSalaryDetails")]
        public async Task<IActionResult> SubmitSalaryDetails([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "A";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth == null || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for submitting salary."));
            }
            try
            {
                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idEmployee))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("User not authenticated."));
                }

                var result = await _salaryService.SubmitSalaryDetails(employeeIds, idSalaryMonth, int.Parse(idEmployee));

                if (!result)
                {
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Failed to submit salary."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess("Salary Submitted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpPost("UndoGeneratedDraftSalary")]
        public async Task<IActionResult> UndoGeneratedDraftSalary([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (string.IsNullOrWhiteSpace(employeeIds))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }
            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "D";

            // Check permission
            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);

            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));

            }
            try
            {
                var deletedCount = await _salaryService.UndoGeneratedDraftSalary(employeeIds, idSalaryMonth);

                if (deletedCount == 0)
                {
                    return Ok(ApiResponseDto<string>.CreateSuccess("No records found to delete."));
                }

                return Ok(ApiResponseDto<string>.CreateSuccess($"Successfully deleted {deletedCount} draft salary records."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }




        [HttpGet("GetSalaryGeneratedDetails")]
        public async Task<IActionResult> GetSalaryGeneratedDetails([FromQuery] int? idSalaryMonth = null, [FromQuery] string? dropdownFilter = null)
        {
            if (!idSalaryMonth.HasValue || idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for retrieving the salary generation details list."));
            }

            try
            {
                var salaryList = await _salaryService.GetSalaryGeneratedDetails(idSalaryMonth, dropdownFilter);

                if (salaryList == null || !salaryList.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDetailsDto>>.CreateSuccess(Enumerable.Empty<SalaryGenerationDetailsDto>(), "No salary records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalaryGenerationDetailsDto>>.CreateSuccess(salaryList, "Salary generation details list retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        [HttpGet("ExportSalaryGenerationDetails")]
        public async Task<IActionResult> ExportSalaryGenerationDetails([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (string.IsNullOrEmpty(employeeIds))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary generation list."));
            }

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "A";

            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);
            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
            }

            try
            {
                var salaryHeads = await _salaryservice.GetSalaryHeadList();
                var result = (await _salaryService.ExportSalaryGenerationDetails(employeeIds, idSalaryMonth)) as IEnumerable<dynamic>;

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<string>.CreateFailure("No data found."));
                }

                var earnings = salaryHeads.Where(h => h.HeadType == "EARNING").Select(h => h.SalaryHeadCode).ToList();
                var deductions = salaryHeads.Where(h => h.HeadType == "DEDUCTION").Select(h => h.SalaryHeadCode).ToList();



                HashSet<string> uniqueEarnings = new HashSet<string>();
                HashSet<string> uniqueDeductions = new HashSet<string>();

                foreach (var r in result.Where(r => r != null)) // Avoid null values
                {
                    var dict = (IDictionary<string, object>)r;

                    foreach (var e in earnings.Where(e => dict.ContainsKey(e)))
                    {
                        uniqueEarnings.Add(e); // Only add earnings present in data
                    }

                    foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                    {
                        uniqueDeductions.Add(d); // Only add deductions present in data
                    }
                }





                // Create Excel package
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Required for EPPlus
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Salary Report");

                    int col = 1;
                    worksheet.Cells[1, col++].Value = "EmployeeCode";
                    worksheet.Cells[1, col++].Value = "EmployeeName";
                    worksheet.Cells[1, col++].Value = "Designation";
                    worksheet.Cells[1, col++].Value = "Joining Date";


                    // Apply header row color
                    using (var range = worksheet.Cells[1, 1, 1, col])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }


                    foreach (var e in uniqueEarnings)
                    {
                        worksheet.Cells[1, col].Value = e;

                        // Apply earning color
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

                        col++;
                    }


                    worksheet.Cells[1, col].Value = "TotalEarnings";
                    // Color for Total Earnings column
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    col++;

                    foreach (var d in uniqueDeductions)
                    {
                        worksheet.Cells[1, col].Value = d;

                        // Apply deduction color
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

                        col++;
                    }

                    worksheet.Cells[1, col].Value = "TotalDeductions";

                    // Color for Total Deductions column
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

                    col++;

                    worksheet.Cells[1, col].Value = "NetSalary";

                    // Color for Net Salary column
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                    col++;

                    worksheet.Cells[1, col].Value = "Status";

                    int row = 2;

                    foreach (var r in result)
                    {
                        if (r == null) continue;

                        col = 1;
                        worksheet.Cells[row, col++].Value = r.EmployeeCode;
                        worksheet.Cells[row, col++].Value = r.EmployeeName;
                        worksheet.Cells[row, col++].Value = r.DesignationName;
                        worksheet.Cells[row, col++].Value = r.JoiningDate?.ToString("dd-MM-yyyy");

                        var dict = (IDictionary<string, object>)r;
                        foreach (var e in earnings.Where(e => dict.ContainsKey(e)))
                        {
                            if (dict.ContainsKey(e) && dict[e] != null)
                            {
                                decimal value = Convert.ToDecimal(dict[e]);
                                worksheet.Cells[row, col].Value = value; // Store as a numeric value
                            }
                            else
                            {
                                worksheet.Cells[row, col].Value = 0.00m; // Default to numeric 0.00
                            }

                            // Apply formatting for two decimal places
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";

                            // Right align the cell content
                            worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

                            col++; // Move to the next column
                        }
                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count - 1)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        string totalEarningsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                        col++;




                        foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        {
                            if (dict.ContainsKey(d) && dict[d] != null)
                            {
                                decimal value = Convert.ToDecimal(dict[d]);
                                worksheet.Cells[row, col].Value = value; // Store as a numeric value
                            }
                            else
                            {
                                worksheet.Cells[row, col].Value = 0.00m; // Default to numeric 0.00
                            }

                            // Apply formatting for two decimal places
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";

                            // Right align the cell content
                            worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                            col++; // Move to the next column
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count+1)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + uniqueDeductions.Count )}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        string totalDeductionsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                        col++;


                        worksheet.Cells[row, col].Formula = $"{totalEarningsColLetter}{row} - {totalDeductionsColLetter}{row}";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                        col++;

                        worksheet.Cells[row, col++].Value = r.Status;

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0; // Reset stream position for reading

                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = "Salary_Report.xlsx";

                    // Convert stream directly to byte array without using another MemoryStream
                    var fileBytes = stream.ToArray();

                    return Ok(new
                    {
                        FileName = fileName,
                        FileType = contentType, // Use the already defined contentType
                        FileContent = Convert.ToBase64String(fileBytes) // Convert to Base64 string
                    });



                    //return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpPost("UploadSalaryGenerationDetails")]
        public async Task<IActionResult> UploadSalaryGenerationDetails(UploadSalaryGenerationDetailsDto uploadSalaryGenerationDetails)
        {

            var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(IdEmployee))
            {
                return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
            }

            var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
            var actionType = "U";

            var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);
            if (!hasPermission)
            {
                return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
            }

            try
            {

                if (uploadSalaryGenerationDetails == null || uploadSalaryGenerationDetails.EmployeeSalaryJsonData.Count == 0)
                {
                    return BadRequest("Invalid input data");
                }

                List<SalaryUploadResponseDto> response = await _salaryService.UploadSalaryDetails(uploadSalaryGenerationDetails, int.Parse(IdEmployee));

                if (response.Count > 0)
                    return Ok(ApiResponseDto<List<SalaryUploadResponseDto>>.CreateSuccess(response, "Salary details uploaded successfully."));
                else
                    return StatusCode(500, ApiResponseDto<string>.CreateFailure("Error processing salary details."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        }
}
