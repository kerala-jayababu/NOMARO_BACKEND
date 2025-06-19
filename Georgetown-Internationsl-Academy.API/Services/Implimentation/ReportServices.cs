using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Borders;
using iText.StyledXmlParser.Jsoup.Nodes;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iText.Layout;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using Paragraph = iText.Layout.Element.Paragraph;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout.Properties;
using PageSize = iTextSharp.text.PageSize;
using System;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class ReportServices : IReportServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BankServices> _logger;
        public ReportServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<IEnumerable<ReportsMasterDto>> GetReportMasters()
        {
            try
            {
                var reports = await (from report in _dbContext.ReportsMaster
                                      orderby report.OrderNumber
                                      select new ReportsMasterDto
                                      {
                                          idReport = report.idReport,
                                          ReportName = report.ReportName,
                                          OrderNumber = report.OrderNumber,
                                          idParentReport = report.idParentReport,
                                          StoredProcName = report.StoredProcName,
                                          ReportTitle = report.ReportTitle,
                                          PrintOrientation = report.PrintOrientation,
                                          RowsInaPage = report.RowsInaPage,
                                          RowHeight = report.RowHeight,
                                          RemoveColumnIfNoData = report.RemoveColumnIfNoData,
                                          IncludeSLNO = report.IncludeSLNO,
                                          ViewableAdminOnly = report.ViewableAdminOnly,
                                          idPermissionEmployeesList = report.idPermissionEmployeesList,
                                          Enabled = report.Enabled,
                                          MergeColumnDetails = report.MergeColumnDetails,
                                          HeaderRequired = report.HeaderRequired,
                                          PDFViewable = report.PDFViewable
                                      }).ToListAsync();

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ReportsMasters");
                throw;
            }
        }

        public async Task<IEnumerable<ReportConditionsDto>> GetReportConditionById(int id)
        {

            var reportConditions = await (from report in _dbContext.ReportConditions
                                          where report.IdReport == id
                                 orderby report.OrderNumber
                                 select new ReportConditionsDto
                                 {
                                     IdReportCondition = report.IdReportCondition,
                                     IdReport = report.IdReport,
                                     ConditionName = report.ConditionName,
                                     SPParameterName = report.SPParameterName,
                                     ControlType = report.ControlType,
                                     DataType = report.DataType,
                                     MandatoryFlag = report.MandatoryFlag,
                                     TableName = report.TableName,
                                     ValueColumn = report.ValueColumn,
                                     DisplayColumn = report.DisplayColumn,
                                     ValidValues = report.ValidValues,
                                     DefaultValue = report.DefaultValue,
                                     WhereCondition = report.WhereCondition,
                                     OrderNumber = report.OrderNumber,
                                     DefaultTime = report.DefaultTime
                                 }).ToListAsync();

            return reportConditions;
        }

        public async Task<IEnumerable<ReportColumnsDto>> GetReportColumnsById(int IdReport)
        {

            var reportColumns = await (from report in _dbContext.ReportColumns
                                          where report.IdReport == IdReport
                                            orderby report.IdReportCondition
                                          select new ReportColumnsDto
                                          {
                                              IdReportCondition = report.IdReportCondition,
                                              IdReport =  report.IdReport,
                                              ColumnName = report.ColumnName,
                                              DataType=report.DataType,
                                              Alignment = report.Alignment,
                                              WidthInPixels = report.WidthInPixels,
                                              TotalRequired=    report.TotalRequired                                              
                                          }).ToListAsync();

            return reportColumns;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(StoredProcedureDto request)
        {
            var result = new List<Dictionary<string, object>>();

            using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = request.StoredProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            foreach (var param in request.Parameters)
            {
                var value = ConvertJsonElement(param.Value);
                var sqlParam = new SqlParameter(param.Key, value ?? DBNull.Value);
                command.Parameters.Add(sqlParam);
            }
            await _dbContext.Database.OpenConnectionAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                }
                result.Add(row);
            }

            await _dbContext.Database.CloseConnectionAsync();

            return result;
        }

        // Helper: Converts JsonElement to native .NET type
        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // Try DateTime parse
                    if (DateTime.TryParse(element.GetString(), out var dateVal))
                        return dateVal;
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intVal))
                        return intVal;
                    if (element.TryGetDecimal(out var decVal))
                        return decVal;
                    return element.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return DBNull.Value;

                default:
                    return element.GetRawText(); // Fallback for object/array
            }
        }

        public async Task<List<dynamic>> GetReportsTableValue(string tableName, string valueColumn, string displayColumn)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    var query = $@"
                SELECT [{valueColumn}] AS valueColumn, [{displayColumn}] AS displayColumn 
                FROM [{tableName}]
                ORDER BY [{valueColumn}]
            ";

                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync(query);
                    return result.ToList();
                }
            }
            catch
            {
                return new List<dynamic>(); // return empty list on error
            }
        }


        #region GetIncomeTax Report
        public async Task<byte[]> GenerateIncomeTaxReportAsync(int payrollId, CompanyDetails compDetails, string taxMonth)
        {
            List<EmployeeTaxDetail> employees = new();

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<EmployeeTaxDetail>(
                    "Report_GUYANA_INCOMETAX",
                    new { SalaryMonth = payrollId },
                    commandType: CommandType.StoredProcedure
                );

                employees = result.ToList();
            }
            catch
            {
                // Log the exception if needed
                return Array.Empty<byte>();
            }

            return CreatePdf(employees, compDetails, taxMonth);
        }

        public byte[] CreatePdf(List<EmployeeTaxDetail> data, CompanyDetails compDetails, string taxMonth)
        {
            using var ms = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(ms);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var doc = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);
            doc.SetMargins(36, 36, 36, 36);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            doc.Add(new Paragraph("GUYANA - INCOME TAX")
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // TIN

 
            // Create a 2-column table: left = Tax Office Address, right = Company Info + TIN
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(10);

            // Left Cell (Tax Office Address)
            var leftCell = new Cell().SetBorder(Border.NO_BORDER);

            leftCell.Add(new Paragraph("To")
                .SetFont(normal)
                .SetFontSize(10)
                .SetMarginBottom(0));

            string fixedAddress = compDetails.TaxOfficeAddress.Replace("\\n", "\n");
            foreach (var line in fixedAddress.Split('\n'))
            {
                leftCell.Add(new Paragraph(line.Trim())
                    .SetFont(normal)
                    .SetFontSize(10)
                    .SetMarginBottom(0)
                    .SetMultipliedLeading(0.9f));
            }

            // Right Cell (TIN + Company Info)
            var rightCell = new Cell().SetBorder(Border.NO_BORDER);
            rightCell.Add(new Paragraph("TIN : " + compDetails.TINNumber)
    .SetFont(normal)
    .SetFontSize(10)
    .SetTextAlignment(TextAlignment.RIGHT));

            Paragraph companyInfo = new Paragraph()
     .Add(new Text(compDetails.CompanyName + "\n").SetFont(normal))
     .Add(new Text(compDetails.Address.Replace("\\n", "\n")).SetFont(normal))
     .SetFontSize(10)
     .SetTextAlignment(TextAlignment.LEFT)
     .SetMultipliedLeading(0.9f);

            rightCell.Add(companyInfo);          

            headerTable.AddCell(leftCell);
            headerTable.AddCell(rightCell);

            // Add table to document
            doc.Add(headerTable);
            // Subtitle
            doc.Add(new Paragraph("RETURN OF DEDUCTIONS OF TAX BY AN EMPLOYER")
     .SetFont(bold)
     .SetFontSize(11)
     .SetTextAlignment(TextAlignment.CENTER)
     .SetMarginTop(10)
       .SetMarginBottom(0)); // add just a little space if needed

            doc.Add(new Paragraph("(Sec. 117 (1) of the Income Tax Act)")
                .SetFont(normal)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(0)
                .SetMarginBottom(0));

            doc.Add(new Paragraph("For the month of " + taxMonth)
                .SetFont(normal)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(0));

            doc.Add(new Paragraph(" "));

            // Table setup
            float[] columnWidths = { 3, 10, 18, 10, 10, 10, 10, 10 };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

            Color bgGray = ColorConstants.LIGHT_GRAY;
            Color border = ColorConstants.GRAY;

            string[] headers = { "No", "TIN", "Name", "Total Income (G$)", "Statutory Deduction", "NIS Employee Contribution", "Medical & Life Insurance", "Income Tax Deducted" };

            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                var alignment =TextAlignment.LEFT;

                table.AddHeaderCell(new Cell()
                   .Add(new Paragraph(header).SetFont(bold).SetFontSize(9))
                   .SetBackgroundColor(bgGray)
                   .SetBorder(new SolidBorder(border, 0.5f))
                   .SetTextAlignment(alignment));
            }

            // Data rows
            int sl = 1;
            decimal totalIncome = 0, totalStatutory = 0, totalNIS = 0, totalTax = 0;

            foreach (var row in data)
            {
                decimal totalIncomeVal = row.TotalIncome ?? 0;
                decimal deductionVal = row.Deduction ?? 0;
                decimal nisVal = row.NIS ?? 0;
                decimal mlieVal = row.MLIE ?? 0;
                decimal incomeTaxVal = row.IncomeTax ?? 0;
                totalIncome += totalIncomeVal;
                totalStatutory += deductionVal;
                totalNIS += nisVal;
                totalTax += incomeTaxVal;

                table.AddCell(CreateCell(sl++.ToString()));
                table.AddCell(CreateCell(row.TINNumber ?? "", false, TextAlignment.LEFT)); 
                table.AddCell(CreateCell(row.EmployeeName ?? "", false, TextAlignment.LEFT));
                table.AddCell(CreateCell(totalIncomeVal.ToString("N2")));
                table.AddCell(CreateCell(deductionVal.ToString("N2")));
                table.AddCell(CreateCell(nisVal.ToString("N2")));
                table.AddCell(CreateCell(mlieVal.ToString("N2")));
                table.AddCell(CreateCell(incomeTaxVal.ToString("N2")));
            }
       

            doc.Add(table);

            // Signature block
            // Signature block
            if (compDetails.SignatureImage != null && compDetails.SignatureImage.Length > 0)
            {
                // Signature image
                //byte[] signImgBytes = Convert.FromBase64String(compDetails.SignatureImageBase64);
                //File.WriteAllBytes("C:\\Sandeep\\test_signature.jpg", signImgBytes);
                ImageData imgData = ImageDataFactory.Create(compDetails.SignatureImage);
                var signature = new iText.Layout.Element.Image(imgData)
                    .ScaleToFit(100f, 40f)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);

                // Signer name
                var signerName = new Paragraph(compDetails.TaxAuthorizedPersonName)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT);

                // Left-side info: Date + IRD No.
                var leftInfo = new Paragraph($"Date : {DateTime.Now:MM-dd-yyyy}\nI.R.D. NO. 5")
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.LEFT);

                // Create 2-column table: 50/50 width
                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth();

                // Left cell (no border, left-aligned)
                var leftCesll = new Cell()
                    .Add(leftInfo)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.LEFT);

                // Right cell (no border, image + name, right-aligned)
                var rightCells = new Cell()
                    .Add(signature)
                    .Add(signerName)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT);

                signTable.AddCell(leftCesll);
                signTable.AddCell(rightCells);

                doc.Add(new Paragraph(" ").SetHeight(10)); // Optional spacing
                doc.Add(signTable);
            }






            doc.Close();
            return ms.ToArray();
        }

        // Helper to simplify cell creation
        private Cell CreateCell(string content, bool bold = false, TextAlignment alignment = TextAlignment.RIGHT)
        {
            var font = bold ? PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD) : PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            Color border = ColorConstants.GRAY;

            return new Cell()
         .Add(new Paragraph(content).SetFont(font).SetFontSize(9))
         .SetTextAlignment(alignment)
         .SetBorder(new SolidBorder(border, 0.5f));
        }
        #endregion

        public async Task<byte[]> GenerateNISReportAsync(int payrollId, string ageGroup, CompanyDetails compDetails, string salaryMonth)
        {
            List<EmployeeContributionDetails> employees = new();

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var result = await connection.QueryAsync<EmployeeContributionDetails>(
                    "Report_GUYANA_NIS",
                    new { SalaryMonth = payrollId, AgeGroup = ageGroup },
                    commandType: CommandType.StoredProcedure
                );

                employees = result.ToList();
            }
            catch (Exception ex)
            {
                // Optionally log
                return Array.Empty<byte>();
            }

            return CreateNISPdf(employees, compDetails, salaryMonth,ageGroup );
        }

        public byte[] CreateNISPdf(List<EmployeeContributionDetails> employees, CompanyDetails compDetails, string salaryMonth, string ageGroup)
        {
            using var ms = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(ms);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var doc = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);
            doc.SetMargins(36, 36, 36, 36);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            doc.Add(new Paragraph("NATIONAL INSURANCE AND SOCIAL SECURITY SCHEME")
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("GUYANA CONTRIBUTION SUMMARY")
                .SetFont(bold).SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(" "));

            // Employer details (table layout)
            var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 5f, 10f }))
                .UseAllAvailableWidth()
                .SetMarginTop(10)
                .SetMarginBottom(10);

            infoTable.AddCell(CreateInfoCell("NAME OF EMPLOYER/BUSINESS:", normal));
            infoTable.AddCell(CreateInfoCell(compDetails.CompanyName, normal));

            infoTable.AddCell(CreateInfoCell("ADDRESS OF BUSINESS:", normal));
            infoTable.AddCell(CreateInfoCell(compDetails.Address, normal));

            infoTable.AddCell(CreateInfoCell("EMPLOYER'S REGISTRATION NUMBER:", normal));
            infoTable.AddCell(CreateInfoCell(compDetails.RegNumber, normal));

            infoTable.AddCell(CreateInfoCell("CONTRIBUTION FOR THE PERIOD:", normal));
            infoTable.AddCell(CreateInfoCell(salaryMonth, normal));

            doc.Add(infoTable);



            // Accumulate totals
            decimal totalActual = 0, totalInsurable = 0, totalEmployer = 0, totalEmployee = 0;
            foreach (var e in employees)
            {
                totalActual += e.ActualEarnings;
                totalInsurable += e.InsurableEarnings;
                totalEmployer += e.EmployerContribution;
                totalEmployee += e.EmployeeContribution;
            }

            // Insert summary block and section header
            decimal totalPayable = totalEmployer + totalEmployee;
            CreateConsTableBlock(doc, totalInsurable, totalPayable,ageGroup, employees.Count);
            doc.Add(new Paragraph(" "));
            AddSectionHeader(doc);

            // Table setup
            float[] columnWidths = { 3, 3, 4, 4, 4, 3, 3 };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

            Color bgGray = ColorConstants.LIGHT_GRAY;
            Color border = ColorConstants.GRAY;

            string[] headers = (ageGroup == "ABOVE60")
                ? new[] { "Surname", "First Name", "NIS No", "Actual Earnings", "Insurable Earnings", "Employer 1.5%", "Employee 0%" }
                : new[] { "Surname", "First Name", "NIS No", "Actual Earnings", "Insurable Earnings", "Employer 8.4%", "Employee 5.6%" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9))
                    .SetBackgroundColor(bgGray)
                    .SetBorder(new SolidBorder(border, 0.5f))
                    .SetTextAlignment(TextAlignment.LEFT));
            }

            // Data rows
            foreach (var e in employees)
            {
                string[] names = (e.EmployeeName ?? "").Split(' ', 2);
                string firstName = names.Length > 1 ? names[0] : "";
                string surname = names.Length > 1 ? names[1] : names[0];

                table.AddCell(CreateCellForNIS(surname));
                table.AddCell(CreateCellForNIS(firstName));
                table.AddCell(CreateCellForNIS(e.NISNumber ?? ""));
                table.AddCell(CreateCellForNIS(e.ActualEarnings.ToString("N2"), TextAlignment.RIGHT));
                table.AddCell(CreateCellForNIS(e.InsurableEarnings.ToString("N2"), TextAlignment.RIGHT));
                table.AddCell(CreateCellForNIS(e.EmployerContribution.ToString("N2"), TextAlignment.RIGHT));
                table.AddCell(CreateCellForNIS(e.EmployeeContribution.ToString("N2"), TextAlignment.RIGHT));
            }

            // Totals row
            table.AddCell(new Cell(1, 3)
    .Add(new Paragraph("TOTAL").SetFont(bold))
    .SetTextAlignment(TextAlignment.CENTER)
    .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f)));

            table.AddCell(CreateCellForNIS(totalActual.ToString("N2"), TextAlignment.RIGHT, bold)
                .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f)));

            table.AddCell(CreateCellForNIS(totalInsurable.ToString("N2"), TextAlignment.RIGHT, bold)
                .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f)));

            table.AddCell(CreateCellForNIS(totalEmployer.ToString("N2"), TextAlignment.RIGHT, bold)
                .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f)));

            table.AddCell(CreateCellForNIS(totalEmployee.ToString("N2"), TextAlignment.RIGHT, bold)
                .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f)));

            doc.Add(table);
            doc.Close();
            return ms.ToArray();
        }

        private Cell CreateCellForNIS(string text, TextAlignment align = TextAlignment.LEFT, iText.Kernel.Font.PdfFont font = null)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font ?? PdfFontFactory.CreateFont(StandardFonts.HELVETICA)).SetFontSize(9))
                .SetTextAlignment(align)
                .SetBorder(new SolidBorder(ColorConstants.GRAY, 0.5f));
        }

        private void CreateConsTableBlock(iText.Layout.Document doc, decimal totalNIS,decimal totalPayabel, string ageGroup, int employeeCount)
        {
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var gray = new DeviceRgb(150, 150, 150);
            float rowHeight = 20f;

            Table summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 5f, 3f, 2f, 3f })).UseAllAvailableWidth();

            var left = new Cell(2, 1)
                .Add(new Paragraph($"Amount Payable G$ {totalPayabel.ToString("N2")}").SetFont(boldFont).SetFontSize(9))
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetHeight(rowHeight * 2)
                .SetBorderRight(new SolidBorder(gray, 0.5f))
                .SetBorderLeft(Border.NO_BORDER)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderBottom(Border.NO_BORDER);

            summaryTable.AddCell(left);

            summaryTable.AddCell(CreateSummaryCell("Employees Age Class", normalFont, TextAlignment.LEFT, gray));
            summaryTable.AddCell(CreateSummaryCell("NO", normalFont, TextAlignment.CENTER, gray));
            summaryTable.AddCell(CreateSummaryCell("Amount", normalFont, TextAlignment.RIGHT, gray));

            string ageClass = ageGroup == "ABOVE60" ? "60 and above" : "Age 16-59 Years";
            summaryTable.AddCell(CreateSummaryCell(ageClass, normalFont, TextAlignment.LEFT, gray));
            summaryTable.AddCell(CreateSummaryCell(employeeCount.ToString(), normalFont, TextAlignment.CENTER, gray));
            summaryTable.AddCell(CreateSummaryCell(totalNIS.ToString("N2"), normalFont, TextAlignment.RIGHT, gray));

            doc.Add(summaryTable);
        }

        private Cell CreateSummaryCell(string text, iText.Kernel.Font.PdfFont font, TextAlignment align, DeviceRgb borderColor)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetTextAlignment(align)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(new SolidBorder(borderColor, 0.5f));
        }

        private void AddSectionHeader(iText.Layout.Document doc)
        {
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var gray = ColorConstants.LIGHT_GRAY;
            var border = ColorConstants.GRAY;

            var sectionHeader = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3, 4, 4, 4, 3, 3 }))
                .UseAllAvailableWidth();

            sectionHeader.AddCell(new Cell(1, 5)
                .Add(new Paragraph("Particulars of Employees").SetFont(boldFont).SetFontSize(9))
                .SetBackgroundColor(gray)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new SolidBorder(border, 0.5f)));

            sectionHeader.AddCell(new Cell(1, 2)
                .Add(new Paragraph("Contributions").SetFont(boldFont).SetFontSize(9))
                .SetBackgroundColor(gray)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new SolidBorder(border, 0.5f)));

            doc.Add(sectionHeader);
        }
        private Cell CreateInfoCell(string text, iText.Kernel.Font.PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPaddingBottom(4);
        }






    }
}
