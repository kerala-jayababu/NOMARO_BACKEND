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
using System.IO.Compression;
using ZipCompressionLevel = System.IO.Compression.CompressionLevel;
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
            var currencyConversions = await _salaryService.CheckcurrencyConversions();
            if (!currencyConversions)
            {
                return StatusCode(
                    403,
                    ApiResponseDto<string>.CreateFailure(
                        "Latest currency conversion details from USD to GYD are not available. Please configure them before proceeding with salary generation."
                    )
                );
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


        [HttpPost("GetSalaryapprovalValue")]
        public async Task<IActionResult> GetSalaryapprovalValue()
        {
            try
            {
                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(idEmployee) || !int.TryParse(idEmployee, out int employeeId))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found or invalid."));
                }

                var result = await _salaryService.GetSalaryapprovalValue(employeeId);

                if (result == null)
                {
                    return Ok(ApiResponseDto<object>.CreateSuccess(null, "No approval status found."));
                }

                return Ok(ApiResponseDto<object>.CreateSuccess(result, "Salary approval value retrieved successfully."));
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
            var currencyConversions = await _salaryService.CheckcurrencyConversions();
            if (!currencyConversions)
            {
                return StatusCode(
                    403,
                    ApiResponseDto<string>.CreateFailure(
                        "Latest currency conversion details from USD to GYD are not available. Please configure them before proceeding with salary generation."
                    )
                );
            }
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

                foreach (var r in result.Where(r => r != null))
                {
                    var dict = (IDictionary<string, object>)r;

                    foreach (var e in earnings.Where(e => dict.ContainsKey(e)))
                        uniqueEarnings.Add(e);

                    foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        uniqueDeductions.Add(d);
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Salary Report");

                    int col = 1;
                    worksheet.Cells[1, col++].Value = "EmployeeCode";
                    worksheet.Cells[1, col++].Value = "EmployeeName";
                    worksheet.Cells[1, col++].Value = "Designation";
                    worksheet.Cells[1, col++].Value = "Joining Date";

                    using (var range = worksheet.Cells[1, 1, 1, col])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    foreach (var e in uniqueEarnings)
                    {
                        worksheet.Cells[1, col].Value = e;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;
                    }

                    worksheet.Cells[1, col].Value = "TotalEarnings";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    col++;

                    foreach (var d in uniqueDeductions)
                    {
                        worksheet.Cells[1, col].Value = d;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;
                    }

                    worksheet.Cells[1, col].Value = "TotalDeductions";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                    col++;

                    worksheet.Cells[1, col].Value = "NetSalary";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                    col++;

                    worksheet.Cells[1, col++].Value = "Status";

                    worksheet.Cells[1, col++].Value = "ChildTaxCredit";
                    worksheet.Cells[1, col++].Value = "FinalTaxableIncome";
                    worksheet.Cells[1, col++].Value = "FinalTaxAmount";
                    worksheet.Cells[1, col++].Value = "TaxReturn";
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
                            decimal value = dict[e] != null ? Convert.ToDecimal(dict[e]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count - 1)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalEarningsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;

                        foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        {
                            decimal value = dict[d] != null ? Convert.ToDecimal(dict[d]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + 1)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + uniqueDeductions.Count)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalDeductionsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;

                        worksheet.Cells[row, col].Formula = $"{totalEarningsColLetter}{row} - {totalDeductionsColLetter}{row}";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                        col++;

                        worksheet.Cells[row, col++].Value = r.Status;
                        // Add tax fields
                        worksheet.Cells[row, col].Value = GetDecimal(dict, "ChildTaxCredit");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxableIncome");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxAmount");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "TaxReturn");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;



                        row++;
                       
                    }

                    // ➕ ADD TOTAL ROW BELOW HERE
                    worksheet.Cells[row, 4].Value = "TOTAL";
                    worksheet.Cells[row, 4].Style.Font.Bold = true;

                    int dataStartRow = 2;
                    int currentCol = 5;
                    for (int i = 0; i < uniqueEarnings.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "0.00";
                    }

                    // Total Earnings
                    string earningsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({earningsCol}{dataStartRow}:{earningsCol}{row - 1})";
                    currentCol++;

                    for (int i = 0; i < uniqueDeductions.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "0.00";
                    }

                    // Total Deductions
                    string deductionsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({deductionsCol}{dataStartRow}:{deductionsCol}{row - 1})";
                    currentCol++;

                    // Net Salary
                    string netSalaryCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({netSalaryCol}{dataStartRow}:{netSalaryCol}{row - 1})";

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = "Salary_Report.xlsx";

                    return Ok(new
                    {
                        FileName = fileName,
                        FileType = contentType,
                        FileContent = Convert.ToBase64String(stream.ToArray())
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }

        decimal GetDecimal(IDictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null && decimal.TryParse(dict[key].ToString(), out var result))
                return result;
            return 0.00m;
        }

        [HttpGet("ExportSalaryGenerationDetailsForApproved")]
        public async Task<IActionResult> ExportSalaryGenerationDetailsForApproved([FromQuery] string employeeIds, [FromQuery] int idSalaryMonth)
        {
            if (string.IsNullOrEmpty(employeeIds))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID list cannot be empty."));
            }
            if (idSalaryMonth <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting salary generation list."));
            }


            try
            {
                var salaryHeads = await _salaryservice.GetSalaryHeadList();
                var result = (await _salaryService.ExportSalaryGenerationDetailsForApproved(employeeIds, idSalaryMonth)) as IEnumerable<dynamic>;

                if (result == null || !result.Any())
                {
                    return Ok(ApiResponseDto<string>.CreateFailure("No data found."));
                }

                var earnings = salaryHeads.Where(h => h.HeadType == "EARNING").Select(h => h.SalaryHeadCode).ToList();
                var deductions = salaryHeads.Where(h => h.HeadType == "DEDUCTION").Select(h => h.SalaryHeadCode).ToList();

                HashSet<string> uniqueEarnings = new HashSet<string>();
                HashSet<string> uniqueDeductions = new HashSet<string>();

                foreach (var r in result.Where(r => r != null))
                {
                    var dict = (IDictionary<string, object>)r;

                    foreach (var e in earnings.Where(e => dict.ContainsKey(e)))
                        uniqueEarnings.Add(e);

                    foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        uniqueDeductions.Add(d);
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Salary Report");

                    int col = 1;
                    worksheet.Cells[1, col++].Value = "EmployeeCode";
                    worksheet.Cells[1, col++].Value = "EmployeeName";
                    worksheet.Cells[1, col++].Value = "Designation";
                    worksheet.Cells[1, col++].Value = "Joining Date";

                    using (var range = worksheet.Cells[1, 1, 1, col])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    foreach (var e in uniqueEarnings)
                    {
                        worksheet.Cells[1, col].Value = e;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;
                    }

                    worksheet.Cells[1, col].Value = "TotalEarnings";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    col++;

                    foreach (var d in uniqueDeductions)
                    {
                        worksheet.Cells[1, col].Value = d;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;
                    }

                    worksheet.Cells[1, col].Value = "TotalDeductions";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                    col++;

                    worksheet.Cells[1, col].Value = "NetSalary";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                    col++;

                    //worksheet.Cells[1, col].Value = "Status";
                    worksheet.Cells[1, col++].Value = "ChildTaxCredit";
                    worksheet.Cells[1, col++].Value = "FinalTaxableIncome";
                    worksheet.Cells[1, col++].Value = "FinalTaxAmount";
                    worksheet.Cells[1, col++].Value = "TaxReturn";

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
                            decimal value = dict[e] != null ? Convert.ToDecimal(dict[e]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count - 1)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalEarningsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;

                        foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        {
                            decimal value = dict[d] != null ? Convert.ToDecimal(dict[d]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + 1)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + uniqueDeductions.Count)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalDeductionsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;

                        worksheet.Cells[row, col].Formula = $"{totalEarningsColLetter}{row} - {totalDeductionsColLetter}{row}";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                        col++;

                        //worksheet.Cells[row, col++].Value = r.Status;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "ChildTaxCredit");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxableIncome");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxAmount");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "TaxReturn");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        row++;
                    }

                    // ➕ ADD TOTAL ROW BELOW HERE
                    worksheet.Cells[row, 4].Value = "TOTAL";
                    worksheet.Cells[row, 4].Style.Font.Bold = true;

                    int dataStartRow = 2;
                    int currentCol = 5;
                    for (int i = 0; i < uniqueEarnings.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "0.00";
                    }

                    // Total Earnings
                    string earningsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({earningsCol}{dataStartRow}:{earningsCol}{row - 1})";
                    currentCol++;

                    for (int i = 0; i < uniqueDeductions.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "0.00";
                    }

                    // Total Deductions
                    string deductionsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({deductionsCol}{dataStartRow}:{deductionsCol}{row - 1})";
                    currentCol++;

                    // Net Salary
                    string netSalaryCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({netSalaryCol}{dataStartRow}:{netSalaryCol}{row - 1})";

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = "Approved_Salary_Report.xlsx";

                    return Ok(new
                    {
                        FileName = fileName,
                        FileType = contentType,
                        FileContent = Convert.ToBase64String(stream.ToArray())
                    });
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


        //[HttpPost("GeneratePayslipPdf")]
        //public async Task<IActionResult> GeneratePayslipPdf(string employeeID, int? idSalaryMonth = null)
        //{

        //    if (string.IsNullOrWhiteSpace(employeeID))
        //    {
        //        return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID cannot be empty."));
        //    }
        //    if (!idSalaryMonth.HasValue || idSalaryMonth <= 0)
        //    {
        //        return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for generating payslip."));
        //    }
        //    //var IdEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    //if (string.IsNullOrEmpty(IdEmployee))
        //    //{
        //    //    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found."));
        //    //}
        //    //var screenCode = _configuration["ScreenCodes:SalaryGeneration"];
        //    //var actionType = "A";
        //    //var hasPermission = await _roleBasedService.CheckEmployeePermission(int.Parse(IdEmployee), screenCode, actionType);
        //    //if (!hasPermission)
        //    //{
        //    //    return StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action."));
        //    //}
        //    try
        //    {
        //        List<byte[]> pdfFiles = new List<byte[]>();
        //        PaySlipGeneratorDto ps = new PaySlipGeneratorDto();

        //        var employeeIDs = employeeID.Split(',').Select(e => e.Trim()).ToList();
        //        var payslipData = await _salaryService.GeneratePayslipPdf(employeeID, idSalaryMonth.Value);

        //        if (payslipData == null || !payslipData.Any())
        //        {
        //            return Ok(ApiResponseDto<string>.CreateFailure("No data found for the given employees."));
        //        }

        //        foreach (var emp in payslipData)
        //        {
        //            using (MemoryStream pdfStream = new MemoryStream()) // Use "using" to ensure disposal
        //            {
        //                ps.GeneratePayslipPdf(emp, pdfStream);
        //                //pdfStream.Position = 0; // Reset the stream position to the beginning

        //                pdfFiles.Add(pdfStream.ToArray()); // Convert to byte array before disposing
        //            }
        //        }

        //        if (pdfFiles.Count == 0)
        //        {
        //            return Ok(ApiResponseDto<string>.CreateFailure("No data found for the given employees."));
        //        }
        //        else if (pdfFiles.Count == 1)
        //        {
        //            return File(pdfFiles[0], "application/pdf", $"Payslip_{employeeID[0]}.pdf");
        //        }
        //        else
        //        {
        //            using (MemoryStream zipStream = new MemoryStream())
        //            {
        //                using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        //                {
        //                    for (int i = 0; i < pdfFiles.Count; i++)
        //                    {

        //                        var entry = zip.CreateEntry($"Payslip_{employeeIDs[i]}.pdf", ZipCompressionLevel.Optimal);
        //                        using (var entryStream = entry.Open())
        //                        {
        //                            entryStream.Write(pdfFiles[i], 0, pdfFiles[i].Length);
        //                        }
        //                    }
        //                }
        //                return File(zipStream.ToArray(), "application/zip", "Payslips.zip");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
        //    }
        //}

        [HttpPost("GeneratePayslipPdf")]
        public async Task<IActionResult> GeneratePayslipPdf(string idEmployeeSalary)
        {
            if (string.IsNullOrWhiteSpace(idEmployeeSalary))
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Employee ID cannot be empty."));
            }

            try
            {
                PaySlipGeneratorDto ps = new PaySlipGeneratorDto();
                List<byte[]> pdfFiles = new List<byte[]>();
                List<string> fileNames = new List<string>();

                var payslipData = await _salaryService.GeneratePayslipPdf(idEmployeeSalary);

                if (payslipData == null || !payslipData.Any())
                {
                    return Ok(ApiResponseDto<string>.CreateFailure("No data found for the given employees."));
                }

                foreach (var emp in payslipData)
                {
                    using (MemoryStream pdfStream = new MemoryStream())
                    {
                        ps.GeneratePayslipPdf(emp, pdfStream);
                        pdfFiles.Add(pdfStream.ToArray());

                        // Construct file name for each payslip
                        string fileName = $"{emp.EmployeeCode}_{emp.EmployeeName}_{emp.Period}.pdf";
                        fileNames.Add(fileName);
                    }
                }

                if (pdfFiles.Count == 0)
                {
                    return Ok(ApiResponseDto<string>.CreateFailure("No data found for the given employees."));
                }
                else if (pdfFiles.Count == 1)
                {
                    // Return a single PDF file with detailed filename
                    return File(pdfFiles[0], "application/pdf", fileNames[0]);
                }
                else
                {
                    using (MemoryStream zipStream = new MemoryStream())
                    {
                        using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                        {
                            for (int i = 0; i < pdfFiles.Count; i++)
                            {
                                var entry = zip.CreateEntry(fileNames[i], System.IO.Compression.CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                {
                                    entryStream.Write(pdfFiles[i], 0, pdfFiles[i].Length);
                                }
                            }
                        }
                        string timestamp = DateTime.Now.ToString("MMddyyyyHHmmss");
                        string zipFileName = $"Payslips_{timestamp}.zip";
                        return File(zipStream.ToArray(), "application/zip", zipFileName);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpGet("GetSalarySlips")]
        public async Task<IActionResult> GetSalarySlips(
           [FromQuery] int idSalaryMonthFrom,
             [FromQuery] int idSalaryMonthTo,
           [FromQuery] string? searchText = null)
        {
            try
            {
                if (idSalaryMonthFrom == null || idSalaryMonthTo == null)
                {
                    return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Id. Id must be greater than 0 for getting SalarySlips list."));
                }

                IEnumerable<SalarySlipDto>? salarySlip = await _salaryService.GetSalarySlips(idSalaryMonthFrom, idSalaryMonthTo, searchText);

                if (salarySlip == null || !salarySlip.Any())
                {
                    return Ok(ApiResponseDto<IEnumerable<SalarySlipDto>>.CreateSuccess(Enumerable.Empty<SalarySlipDto>(), "No salary slip records found."));
                }

                return Ok(ApiResponseDto<IEnumerable<SalarySlipDto>>.CreateSuccess(salarySlip, "Salary slip retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



    }
}
