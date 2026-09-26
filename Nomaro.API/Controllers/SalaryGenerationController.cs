using Asp.Versioning;
using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO.Compression;
using ZipCompressionLevel = System.IO.Compression.CompressionLevel;
using System.Security.Claims;
using Nomaro.API.Helpers;
using System.Net.Mail;
using System.Diagnostics;
using Nomaro.API.Database;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Controllers
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
        private readonly INotificationConfigService _notificationConfigService;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IOptionService _optionService;
        private readonly IServiceProvider _serviceProvider;


        public SalaryGenerationController(ISalaryGenerationService salaryService, IServiceProvider serviceProvider,INotificationConfigService notificationConfigService, IOptionService optionService,  IConfiguration configuration, ISalaryHeadServices salaryservice, IRoleBasedScreenService roleBasedService, ISalaryHeadServices salaryHeadService)
        {
            _salaryService = salaryService;
            _configuration = configuration;
            _roleBasedService = roleBasedService;
            _salaryHeadService = salaryHeadService;
            _optionService = optionService;
            _serviceProvider = serviceProvider;
            _salaryservice = salaryservice;
            _notificationConfigService = notificationConfigService;
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

                var idEmployee = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(idEmployee) || !int.TryParse(idEmployee, out int employeeId))
                {
                    return Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found or invalid."));
                }

                IEnumerable<SalaryGenerationDto>? salaryList = await _salaryService.GetSalaryConfigs(employeeId,idSalaryMonth,dropdownFilter, idDepartment, idDesignation);

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
            var salarydetails = await _optionService.GetAllSalaryMonths();

            try
            {
                // âœ… Configure which columns should be read-only by name
                var readOnlyColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EmployeeCode",
            "EmployeeName",
            "Designation",
            "Joining Date",
            "TotalEarnings",
            "TotalDeductions",
            "PEN",
            "NetSalary",
            "Status",
            "ChildTaxCredit",
             "FinalTaxableIncome",
             "FinalTaxAmount"
        };

                var salaryHeads = await _salaryservice.GetSalaryHeadList();
                var result = (await _salaryService.ExportSalaryGenerationDetails(employeeIds, idSalaryMonth)) as IEnumerable<dynamic>;

                var salarymonth = salarydetails.Where(x => x.IdSalaryMonth == idSalaryMonth).FirstOrDefault();
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
                    DateTime monthYear = Convert.ToDateTime(salarymonth.SalaryMonthText);
                    string sheetName = monthYear.ToString("MMMM yyyy");
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    int col = 1;
                    // âœ… Track column index by header name
                    var columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    columnIndexMap["EmployeeCode"] = col;
                    worksheet.Cells[1, col++].Value = "EmployeeCode";

                    columnIndexMap["EmployeeName"] = col;
                    worksheet.Cells[1, col++].Value = "EmployeeName";

                    columnIndexMap["Designation"] = col;
                    worksheet.Cells[1, col++].Value = "Designation";

                    columnIndexMap["Joining Date"] = col;
                    worksheet.Cells[1, col++].Value = "Joining Date";

                    using (var range = worksheet.Cells[1, 1, 1, col])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    foreach (var e in uniqueEarnings)
                    {
                        columnIndexMap[e] = col;
                        worksheet.Cells[1, col].Value = e;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;
                    }

                    columnIndexMap["TotalEarnings"] = col;
                    worksheet.Cells[1, col].Value = "TotalEarnings";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[1, col].Style.Font.Bold = true;
                    col++;

                    foreach (var d in uniqueDeductions)
                    {
                        columnIndexMap[d] = col;
                        worksheet.Cells[1, col].Value = d;
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;
                    }

                    columnIndexMap["TotalDeductions"] = col;
                    worksheet.Cells[1, col].Value = "TotalDeductions";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                    worksheet.Cells[1, col].Style.Font.Bold = true;
                    col++;

                    columnIndexMap["NetSalary"] = col;
                    worksheet.Cells[1, col].Value = "NetSalary";
                    worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                    worksheet.Cells[1, col].Style.Font.Bold = true;
                    col++;

                    columnIndexMap["Status"] = col;
                    worksheet.Cells[1, col++].Value = "Status";

                    columnIndexMap["ChildTaxCredit"] = col;
                    worksheet.Cells[1, col++].Value = "ChildTaxCredit";

                    columnIndexMap["FinalTaxableIncome"] = col;
                    worksheet.Cells[1, col++].Value = "FinalTaxableIncome";

                    columnIndexMap["FinalTaxAmount"] = col;
                    worksheet.Cells[1, col++].Value = "FinalTaxAmount";

                    columnIndexMap["TaxReturn"] = col;
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
                            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count - 1)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalEarningsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;

                        foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        {
                            decimal value = dict[d] != null ? Convert.ToDecimal(dict[d]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + 1)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + uniqueDeductions.Count)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalDeductionsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;

                        worksheet.Cells[row, col].Formula = $"{totalEarningsColLetter}{row} - {totalDeductionsColLetter}{row}";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                        col++;

                        worksheet.Cells[row, col++].Value = r.Status;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "ChildTaxCredit");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxableIncome");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxAmount");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "TaxReturn");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        row++;
                    }

                    // âž• TOTAL ROW
                    worksheet.Cells[row, 4].Value = "TOTAL";
                    worksheet.Cells[row, 4].Style.Font.Bold = true;

                    int dataStartRow = 2;
                    int currentCol = 5;
                    for (int i = 0; i < uniqueEarnings.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    }

                    string earningsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({earningsCol}{dataStartRow}:{earningsCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    currentCol++;

                    for (int i = 0; i < uniqueDeductions.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    }

                    string deductionsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({deductionsCol}{dataStartRow}:{deductionsCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    currentCol++;

                    string netSalaryCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({netSalaryCol}{dataStartRow}:{netSalaryCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;

                    // âœ… PROTECTION BLOCK - Unlock all cells, then lock only read-only columns
                    worksheet.Cells[worksheet.Dimension.Address].Style.Locked = false;

                    foreach (var kvp in columnIndexMap)
                    {
                        if (readOnlyColumnNames.Contains(kvp.Key))
                        {
                            worksheet.Column(kvp.Value).Style.Locked = true;
                        }
                    }

                    worksheet.Protection.IsProtected = true;
                    worksheet.Protection.AllowSelectLockedCells = true;
                    worksheet.Protection.AllowSelectUnlockedCells = true;
                    worksheet.Protection.AllowFormatColumns = true; 
                    // worksheet.Protection.SetPassword("yourpassword"); // âœ… Uncomment to add password

                     worksheet.Cells.AutoFitColumns();
                    //worksheet.Cells.AutoFitColumns(10, 30);

                    // âœ… Force minimum width on all columns as safety net
                    //for (int i = 1; i <= worksheet.Dimension.End.Column; i++)
                    //{
                    //    if (worksheet.Column(i).Width < 15)
                    //        worksheet.Column(i).Width = 15;
                    //}
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = $"Salary_Draft Generated_{sheetName}.xlsx";

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
                var salarydetails = await _optionService.GetAllSalaryMonths();
                var salarymonth = salarydetails.Where(x => x.IdSalaryMonth == idSalaryMonth).FirstOrDefault();
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
                    DateTime monthYear = Convert.ToDateTime(salarymonth.SalaryMonthText);
                    string sheetName = monthYear.ToString("MMMM yyyy");

                    // Create worksheet with month-year name
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);
                    worksheet.Cells["A1"].Value = "NOMARO";
                    worksheet.Cells["A2"].Value = "Salary for Review & Approval";
                    worksheet.Cells["A3"].Value = $"Salary Month: {monthYear:MMMM, yyyy}";

                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.Font.Color.SetColor(Color.Black);
                    worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    // A2 â€“ Blue, Bold, Left-aligned
                    worksheet.Cells["A2"].Style.Font.Bold = true;
                    worksheet.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(0, 102, 204)); // Blue
                    worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    // A3 â€“ Gray, Italic, Left-aligned
                    worksheet.Cells["A3"].Style.Font.Italic = true;
                    worksheet.Cells["A3"].Style.Font.Color.SetColor(Color.Gray);
                    worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    //worksheet.Cells["A1:C1"].Merge = true;
                    //worksheet.Cells["A2:C2"].Merge = true;
                    //worksheet.Cells["A3:U3"].Merge = true;
                    for (int i = 1; i <= 3; i++)
                    {
                        //worksheet.Row(i).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Row(i).Style.Font.Bold = true;
                    }
                    int headerRow = 4;
                    int col = 1;
                   
                    worksheet.Cells[headerRow, col].Value = "EmployeeCode";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow,col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "EmployeeName";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "Designation";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "Joining Date";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));

                    using (var range = worksheet.Cells[1, 1, 1, col])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    foreach (var e in uniqueEarnings)
                    {
                        worksheet.Cells[headerRow, col].Value = e;
                        worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                        worksheet.Cells[headerRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;
                    }

                    worksheet.Cells[headerRow, col].Value = "TotalEarnings";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    col++;

                    foreach (var d in uniqueDeductions)
                    {
                        worksheet.Cells[headerRow, col].Value = d;
                        worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                        worksheet.Cells[headerRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;
                    }

                    worksheet.Cells[headerRow, col].Value = "TotalDeductions";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                    col++;

                    worksheet.Cells[headerRow, col].Value = "NetSalary";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                    col++;

                    //worksheet.Cells[1, col].Value = "Status";
                    worksheet.Cells[headerRow, col].Value = "ChildTaxCredit";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    //worksheet.Cells[headerRow, col].Value = "Designation";
                    //worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    //worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "FinalTaxableIncome";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "FinalTaxAmount";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    worksheet.Cells[headerRow, col].Value = "TaxReturn";
                    worksheet.Cells[headerRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    worksheet.Cells[headerRow, col++].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));

                    int row = headerRow + 1;

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
                            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count - 1)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalEarningsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        col++;

                        foreach (var d in deductions.Where(d => dict.ContainsKey(d)))
                        {
                            decimal value = dict[d] != null ? Convert.ToDecimal(dict[d]) : 0.00m;
                            worksheet.Cells[row, col].Value = value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                            col++;
                        }

                        worksheet.Cells[row, col].Formula = $"SUM({ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + 1)}{row}:{ExcelCellAddress.GetColumnLetter(5 + uniqueEarnings.Count + uniqueDeductions.Count)}{row})";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        string totalDeductionsColLetter = ExcelCellAddress.GetColumnLetter(col);
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(218, 226, 243));
                        col++;

                        worksheet.Cells[row, col].Formula = $"{totalEarningsColLetter}{row} - {totalDeductionsColLetter}{row}";
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, col].Style.Font.Bold = true;
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238));
                        col++;

                        //worksheet.Cells[row, col++].Value = r.Status;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "ChildTaxCredit");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxableIncome");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "FinalTaxAmount");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[row, col].Value = GetDecimal(dict, "TaxReturn");
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, col++].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        row++;
                    }

                    // âž• ADD TOTAL ROW BELOW HERE
                    worksheet.Cells[row, 4].Value = "TOTAL";
                    worksheet.Cells[row, 4].Style.Font.Bold = true;

                    int dataStartRow = 2;
                    int currentCol = 5;
                    for (int i = 0; i < uniqueEarnings.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                      
                    }

                    // Total Earnings
                    string earningsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({earningsCol}{dataStartRow}:{earningsCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    currentCol++;

                    for (int i = 0; i < uniqueDeductions.Count; i++, currentCol++)
                    {
                        string colLetter = ExcelCellAddress.GetColumnLetter(currentCol);
                        worksheet.Cells[row, currentCol].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{row - 1})";
                        worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                        worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    }

                    // Total Deductions
                    string deductionsCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({deductionsCol}{dataStartRow}:{deductionsCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";
                    currentCol++;

                    // Net Salary
                    string netSalaryCol = ExcelCellAddress.GetColumnLetter(currentCol);
                    worksheet.Cells[row, currentCol].Formula = $"SUM({netSalaryCol}{dataStartRow}:{netSalaryCol}{row - 1})";
                    worksheet.Cells[row, currentCol].Style.Font.Bold = true;
                    worksheet.Cells[row, currentCol].Style.Numberformat.Format = "#,##0.00";

                    //int footerRow = row + 2;
                    //worksheet.Cells[footerRow, 1].Value = "Generated by";
                    //worksheet.Cells[footerRow + 1, 1].Value = "BM Approved by";
                    //worksheet.Cells[footerRow + 2, 1].Value = "HR Approved by";
                    //worksheet.Cells[footerRow + 3, 1].Value = "Final Approved by";

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = $"Salary for Review_{sheetName}.xlsx";

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


        [HttpGet("GetPayslipObject")]
        public async Task<IActionResult> GetPayslipObject(int idEmployeeSalary)
        {
            if (idEmployeeSalary <= 0)
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid Employee Salary ID."));

            try
            {
                var payslip = await _salaryService.GetPayslipObjectAsync(idEmployeeSalary);

                if (payslip == null)
                    return NotFound(ApiResponseDto<string>.CreateFailure("Payslip not found."));

                return Ok(ApiResponseDto<EmployeePayslipDto>.CreateSuccess(payslip));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }



        [HttpPost("GetPayslipDetailsForLeavePassage")]
        public async Task<IActionResult> GetPayslipDetailsForLeavePassage(int IdEmployee)
        {
            if (IdEmployee <= 0)
            {
                return BadRequest(ApiResponseDto<string>.CreateFailure("Invalid employee ID."));
            }

            try
            {
                // Call your service method to get the payslip details
                var payslipData = await _salaryService.GetPayslipDetailsForLeavePassage(IdEmployee);

                if (payslipData == null)
                {
                    return Ok(ApiResponseDto<EmployeePayslipDto>.CreateFailure("No payslip found for the given employee."));
                }

                // Return the payslip data in a success response
                return Ok(ApiResponseDto<EmployeePayslipDto>.CreateSuccess(payslipData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"));
            }
        }


        private void LogEvent(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            //string source = "Payroll";
            //string logName = "Application";

            //if (!EventLog.SourceExists(source))
            //{
            //    EventLog.CreateEventSource(source, logName);
            //}

            //EventLog.WriteEntry(source, message, type);

            if (EventLog.SourceExists("Payroll"))
            {
                EventLog.WriteEntry("Payroll", message, EventLogEntryType.Information);
            }
        }

        [HttpPost("GenerateNotificationForEmployeeSalary")]
        [AllowAnonymous]
        public IActionResult GenerateNotificationForEmployeeSalary(
         string? idEmployeeSalary)
        {
            if (string.IsNullOrWhiteSpace(idEmployeeSalary))
            {
                return BadRequest(
                    ApiResponseDto<string>.CreateFailure(
                        "Please provide either a list of EmployeeSalary IDs."));
            }

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var salaryService = scope.ServiceProvider.GetRequiredService<ISalaryGenerationService>();
                var notificationConfigService = scope.ServiceProvider.GetRequiredService<INotificationConfigService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SalaryGenerationController>>();
                try
                {
                    var payslipData = await salaryService
                        .GenerateNotificationForEmployeeSalary(idEmployeeSalary);

                    if (payslipData == null || !payslipData.Any())
                    {
                        // nothing to send
                        return;
                    }

                    // grab template once - get entity for helper method
                    var notificationConfig = await dbContext.NotificationsConfig
                        .FirstOrDefaultAsync(c => c.EntityCode == "SALARY");
                    
                    if (notificationConfig == null)
                        throw new InvalidOperationException("Missing SALARY template");

                    // process in parallel, up to 10 at once
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };
                    await Parallel.ForEachAsync(payslipData, parallelOptions, async (emp, ct) =>
                    {
                        try
                        {
                            await salaryService.MarkSalaryEmailInProcessAsync(emp.IdEmployeeSalary);
                            var preLog = $"[IN-PROCESS] Salary email is being prepared for {emp.EmployeeName} (EmployeeCode: {emp.EmployeeCode}, SalaryId: {emp.IdEmployeeSalary})";

                            logger.LogInformation(preLog);
                            //LogEvent(preLog); // Windows Event Viewer
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "Failed to mark EmployeeSalary {SalaryId} as InProcess before sending email.",
                                emp.IdEmployeeSalary);
                            // We still attempt to send the email, but itâ€™s worth logging 
                            // that the status update to "InProcess" failed
                        }


                        // 1) PDF
                        byte[] pdfBytes;
                        using (var ms = new MemoryStream())
                        {
                            var psGen = new PaySlipGeneratorDto();
                            psGen.GeneratePayslipPdf(emp, ms);
                            pdfBytes = ms.ToArray();
                        }

                        // 2) build email body
                        var replacements = new Dictionary<string, string>
                        {
                            { "#EMPLOYEENAME#", emp.EmployeeName ?? "" },
                            { "#SALARYMONTH#", emp.Period ?? "" }
                        };

                        var body = await notificationConfigService.GetProcessedNotificationContentAsync(notificationConfig, replacements);

                        var disbHtml = string.Join("<br/>", emp.BankRemittance.Select(r =>
                        {
                            var amt = r.AmountUSD ?? r.AmountGTD ?? 0m;
                            var sym = r.Currency == "USD" ? "US$"
                                     : r.Currency == "GYD" ? "G$" : "";
                            return $"Bank: {r.BankName}, Account: {r.AccountNumber}, Amount: {sym} {amt:N2}";
                        }));
                        body = body?.Replace(
                            "[[Bank : #BANKNAME#, Account Number : #ACCOUNTNUMBER#, Amount : #AMOUNT#]]",
                            disbHtml
                        );

                      
                        // 4) send
                        var fileName = $"{emp.EmployeeCode}_{emp.EmployeeName}_{emp.Period}.pdf";
                        try
                        {
                            await EmailService.SendMail(
                                emp.EmailID,
                                notificationConfig.EmailSubject,
                                body,
                                pdfBytes,
                                fileName
                            );
                            var postLog = $"[SENT] Payslip email successfully sent to {emp.EmployeeName} ({emp.EmailID}) for SalaryId: {emp.IdEmployeeSalary}";
                            logger.LogInformation(postLog);
                            //LogEvent(postLog);

                            // 3.5) Mark the salary record as "Sent" AFTER email is successfully delivered
                            try
                            {
                                await salaryService.MarkSalaryEmailSentAsync(emp.IdEmployeeSalary);
                            }
                            catch (Exception ex2)
                            {
                                logger.LogError(
                                    ex2,
                                    "Failed to mark EmployeeSalary {SalaryId} as Sent after sending email.",
                                    emp.IdEmployeeSalary);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // If the email fails to send, you might want to update 
                            // EmailStatus = "Failed" (or leave it InProcess),
                            // or log for retry. For simplicity, we just log here:
                            logger.LogError(
                                emailEx,
                                "Error sending payslip email to {Email} for SalaryId {SalaryId}",
                                emp.EmailID,
                                emp.IdEmployeeSalary);
                        }
                    });
                }
                catch (Exception ex)
                {
                    // TODO: log exception
                }
            });

            // immediate response
            return Accepted(ApiResponseDto<string>.CreateSuccess(
                "Payslip generation queued. You will get emails shortly."));
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

