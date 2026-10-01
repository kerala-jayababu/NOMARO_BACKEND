using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Document = iText.Layout.Document;
using Table = iText.Layout.Element.Table;

namespace Nomaro.API.DTO
{
    /// <summary>
    /// Builds the salary slip PDF (A4) in the "ABC India" layout:
    /// company header, title bar with pay month, employee details, earnings / deductions,
    /// net pay with amount in words, employer contributions, year to date, remarks and signatory.
    /// Values that are not available in EmployeePayslipDto are printed blank.
    /// </summary>
    public class PaySlipGeneratorDto
    {
        // Colour pattern (SystemParameters.PAYSLIPCOLORPATTERN): RED, GRAY, BLUE, GREEN, BROWN, VIOLET. Default BLUE.
        // Dark  = title bar / section title backgrounds (white text)
        // Light = column headers, total rows, light section headers (black text)
        // Pale  = amount in words background (black text)
        // Border = table lines
        private static readonly Dictionary<string, (DeviceRgb Dark, DeviceRgb Light, DeviceRgb Pale, DeviceRgb Border)> ColorPatterns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["BLUE"] = (new DeviceRgb(31, 73, 125), new DeviceRgb(221, 235, 247), new DeviceRgb(241, 247, 253), new DeviceRgb(197, 214, 232)),
                ["RED"] = (new DeviceRgb(150, 30, 35), new DeviceRgb(248, 220, 221), new DeviceRgb(253, 242, 242), new DeviceRgb(233, 190, 192)),
                ["GRAY"] = (new DeviceRgb(64, 64, 64), new DeviceRgb(226, 226, 226), new DeviceRgb(244, 244, 244), new DeviceRgb(200, 200, 200)),
                ["GREEN"] = (new DeviceRgb(30, 100, 55), new DeviceRgb(221, 240, 226), new DeviceRgb(242, 249, 244), new DeviceRgb(190, 220, 200)),
                ["BROWN"] = (new DeviceRgb(110, 70, 40), new DeviceRgb(239, 226, 212), new DeviceRgb(250, 245, 239), new DeviceRgb(215, 195, 175)),
                ["VIOLET"] = (new DeviceRgb(85, 50, 130), new DeviceRgb(234, 224, 245), new DeviceRgb(247, 243, 251), new DeviceRgb(210, 195, 230)),
            };
        private const string DefaultColorPattern = "BLUE";

        // Text is only white (on dark backgrounds) or black
        private static readonly Color White = ColorConstants.WHITE;
        private static readonly Color Black = ColorConstants.BLACK;

        // Background / border colours for the current document
        private Color _dark = null!;
        private Color _light = null!;
        private Color _pale = null!;
        private Color _border = null!;

        private static readonly CultureInfo IndianCulture = new CultureInfo("en-IN");

        // Fonts are created per document (an iText font cannot be shared between PDF documents)
        private PdfFont _regular = null!;
        private PdfFont _bold = null!;
        private string _currencySymbol = "₹";

        private const float BorderWidth = 0.6f;
        private const float FontSize = 8.5f;

        public void GeneratePayslipPdf(EmployeePayslipDto payslip, Stream outputStream)
        {
            var writer = new PdfWriter(outputStream);
            writer.SetCloseStream(false);

            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf, PageSize.A4))
            {
                LoadFonts();
                ApplyColorPattern(payslip.PayslipColorPattern);
                document.SetMargins(16, 22, 14, 22);
                document.SetFont(_regular).SetFontSize(FontSize).SetFontColor(Black);

                var earnings = payslip.Earnings ?? new List<EmployeeSalaryDetailsDto>();
                var deductions = payslip.Deductions ?? new List<EmployeeSalaryDetailsDto>();
                var employerContributions = payslip.EmployerContributions ?? new List<EmployeeSalaryDetailsDto>();

                var grossEarnings = earnings.Sum(x => x.AmountG);
                var totalDeductions = deductions.Sum(x => x.AmountG);
                var netPay = grossEarnings - totalDeductions;

                document.Add(CreateCompanyHeader(payslip));
                document.Add(Spacer(5));
                document.Add(CreateTitleBar(payslip));
                document.Add(Spacer(5));
                document.Add(CreateEmployeeDetails(payslip));
                document.Add(Spacer(5));
                document.Add(CreateEarningsAndDeductions(earnings, deductions, grossEarnings, totalDeductions));
                document.Add(Spacer(5));
                document.Add(CreateNetPay(netPay));
                document.Add(Spacer(5));
                document.Add(CreateEmployerContributionsAndYtd(payslip, employerContributions));
                document.Add(Spacer(5));
                document.Add(CreateFooter(payslip));
            }
        }

        #region Fonts

        /// <summary>
        /// Uses a TrueType font that has the Rupee sign (₹); falls back to Helvetica with "Rs." when none is installed.
        /// </summary>
        private void LoadFonts()
        {
            var candidates = new[]
            {
                (Regular: @"C:\Windows\Fonts\arial.ttf", Bold: @"C:\Windows\Fonts\arialbd.ttf"),
                (Regular: @"C:\Windows\Fonts\segoeui.ttf", Bold: @"C:\Windows\Fonts\segoeuib.ttf"),
                (Regular: "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", Bold: "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"),
            };

            foreach (var (regular, bold) in candidates)
            {
                if (File.Exists(regular) && File.Exists(bold))
                {
                    _regular = PdfFontFactory.CreateFont(regular, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    _bold = PdfFontFactory.CreateFont(bold, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    _currencySymbol = "₹";
                    return;
                }
            }

            _regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            _bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            _currencySymbol = "Rs.";
        }

        #endregion

        #region Sections

        private Table CreateCompanyHeader(EmployeePayslipDto payslip)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 60f, 40f })).UseAllAvailableWidth();

            // Left: logo and company name
            var left = NoBorderCell().SetVerticalAlignment(VerticalAlignment.MIDDLE);
            var logoAndName = new Table(UnitValue.CreatePercentArray(new float[] { 1f, 3f })).UseAllAvailableWidth();
            var logoCell = NoBorderCell().SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPaddingRight(8);
            var logo = CreateImage(payslip.logo, payslip.LogoHeightInPayslip > 0 ? Math.Min(payslip.LogoHeightInPayslip, 55f) : 50f);
            if (logo != null)
            {
                logoCell.Add(logo);
            }
            logoAndName.AddCell(logoCell);
            logoAndName.AddCell(NoBorderCell().SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(Para(payslip.CompanyName?.ToUpper(), 15, true, Black)));
            left.Add(logoAndName);
            table.AddCell(left);

            // Right: company name, address, CIN
            var right = NoBorderCell().SetVerticalAlignment(VerticalAlignment.MIDDLE);
            right.Add(Para(payslip.CompanyName, 9.5f, true, Black));
            if (!string.IsNullOrWhiteSpace(payslip.CompanyAddress))
            {
                right.Add(Para(payslip.CompanyAddress, 8.5f, false, Black));
            }
            if (!string.IsNullOrWhiteSpace(payslip.CompanyRegistrationNumber))
            {
                right.Add(Para($"CIN: {payslip.CompanyRegistrationNumber}", 8.5f, false, Black));
            }
            table.AddCell(right);

            return table;
        }

        private Table CreateTitleBar(EmployeePayslipDto payslip)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 69f, 31f })).UseAllAvailableWidth();

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(_dark)
                .SetPaddingTop(3).SetPaddingBottom(3).SetPaddingLeft(14)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(Para("SALARY SLIP", 14, true, White)));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBorderLeft(new SolidBorder(White, 1f))
                .SetBackgroundColor(_dark)
                .SetPaddingTop(3).SetPaddingBottom(3)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(Para("PAY MONTH", 7, false, White).SetTextAlignment(TextAlignment.CENTER))
                .Add(Para(payslip.PayMonthText ?? payslip.Period, 10.5f, true, White).SetTextAlignment(TextAlignment.CENTER)));

            return table;
        }

        private Table CreateEmployeeDetails(EmployeePayslipDto payslip)
        {
            var left = new List<(string Label, string? Value)>
            {
                ("Employee ID", payslip.EmployeeCode),
                ("Employee Name", payslip.EmployeeName),
                ("Designation", payslip.Position),
                ("Department", payslip.Department),
                ("Date of Joining", payslip.DateOfJoining),
                ("Employment Type", payslip.EmploymentType),
                ("Location", payslip.Location),
            };
            var right = new List<(string Label, string? Value)>
            {
                ("PAN No.", payslip.PanNumber),
                ("UAN No.", payslip.UanNumber),
                ("PF No.", payslip.PfNumber),
                ("ESI No.", payslip.EsiNumber),
                ("Bank Name", payslip.BankName),
                ("Bank Account No.", payslip.BankAccountNumber),
                ("Loss of Pay (LOP) Days", payslip.LopDays),
            };

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 32f, 22f, 28f })).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(_border, BorderWidth));

            table.AddHeaderCell(SectionHeaderLight("EMPLOYEE DETAILS", 4));

            var rows = Math.Max(left.Count, right.Count);
            for (var i = 0; i < rows; i++)
            {
                var l = i < left.Count ? left[i] : (Label: "", Value: (string?)null);
                var r = i < right.Count ? right[i] : (Label: "", Value: (string?)null);
                table.AddCell(DetailCell(l.Label));
                table.AddCell(DetailCell(l.Value));
                table.AddCell(DetailCell(r.Label));
                table.AddCell(DetailCell(r.Value));
            }

            return table;
        }

        private Table CreateEarningsAndDeductions(List<EmployeeSalaryDetailsDto> earnings, List<EmployeeSalaryDetailsDto> deductions,
            decimal grossEarnings, decimal totalDeductions)
        {
            var rowCount = Math.Max(earnings.Count, deductions.Count);

            var layout = new Table(UnitValue.CreatePercentArray(new float[] { 49f, 2f, 49f })).UseAllAvailableWidth();
            layout.AddCell(NoBorderCell().SetPadding(0).Add(
                AmountSection("EARNINGS", "Earning Head", earnings, rowCount, "GROSS EARNINGS (A)", grossEarnings)));
            layout.AddCell(NoBorderCell());
            layout.AddCell(NoBorderCell().SetPadding(0).Add(
                AmountSection("DEDUCTIONS", "Deduction Head", deductions, rowCount, "TOTAL DEDUCTIONS (B)", totalDeductions)));
            return layout;
        }

        private Table CreateNetPay(decimal netPay)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 66f, 34f })).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(_border, BorderWidth));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(_light)
                .SetPaddingTop(4).SetPaddingBottom(4).SetPaddingLeft(14)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(Para("NET PAY (A - B)", 11, true, Black)));

            table.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(_dark)
                .SetPaddingTop(4).SetPaddingBottom(4).SetPaddingRight(12)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.RIGHT)
                .Add(Para($"{_currencySymbol} {FormatAmount(netPay)}", 12, true, White).SetTextAlignment(TextAlignment.RIGHT)));

            var words = new Paragraph()
                .SetMargin(0)
                .Add(new Text("Amount in Words:   ").SetFont(_bold).SetFontSize(FontSize).SetFontColor(Black))
                .Add(new Text(AmountInWords(netPay)).SetFont(_regular).SetFontSize(FontSize).SetFontColor(Black));

            table.AddCell(new Cell(1, 2)
                .SetBorder(Border.NO_BORDER)
                .SetBorderTop(new SolidBorder(_border, BorderWidth))
                .SetBackgroundColor(_pale)
                .SetPaddingTop(4).SetPaddingBottom(4).SetPaddingLeft(14)
                .Add(words));

            return table;
        }

        private Table CreateEmployerContributionsAndYtd(EmployeePayslipDto payslip, List<EmployeeSalaryDetailsDto> employerContributions)
        {
            var ytdRows = new List<(string Label, decimal? Amount)>
            {
                ("Total Earnings (YTD)", payslip.TotalEarningsYtd),
                ("Total Deductions (YTD)", payslip.TotalDeductionsYtd),
                ("Net Pay (YTD)", payslip.NetPayYtd),
                ("Employer PF (YTD)", payslip.EmployerPfYtd),
                ("Employer EPS (YTD)", payslip.EmployerEpsYtd),
            };
            var rowCount = Math.Max(employerContributions.Count, ytdRows.Count);

            // Employer contributions
            var contributions = new Table(UnitValue.CreatePercentArray(new float[] { 15f, 59f, 26f })).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(_border, BorderWidth));
            var contributionTitle = new Paragraph().SetMargin(0)
                .Add(new Text("EMPLOYER CONTRIBUTIONS ").SetFont(_bold).SetFontSize(9f).SetFontColor(White))
                .Add(new Text("(For Information Only)").SetFont(_regular).SetFontSize(7.5f).SetFontColor(White));
            contributions.AddHeaderCell(new Cell(1, 3).SetBorder(Border.NO_BORDER).SetBackgroundColor(_dark).SetPadding(6).SetPaddingLeft(8).Add(contributionTitle));
            contributions.AddHeaderCell(ColumnHeader("Sl. No.", TextAlignment.CENTER));
            contributions.AddHeaderCell(ColumnHeader("Contribution Head", TextAlignment.LEFT));
            contributions.AddHeaderCell(ColumnHeader($"Amount ({_currencySymbol})", TextAlignment.RIGHT));
            for (var i = 0; i < rowCount; i++)
            {
                var row = i < employerContributions.Count ? employerContributions[i] : null;
                contributions.AddCell(BodyCell(row != null ? (i + 1).ToString() : "", TextAlignment.CENTER));
                contributions.AddCell(BodyCell(row?.Description, TextAlignment.LEFT));
                contributions.AddCell(BodyCell(row != null ? FormatAmount(row.AmountG) : "", TextAlignment.RIGHT));
            }

            // Year to date
            var ytdTitle = string.IsNullOrWhiteSpace(payslip.YtdPeriodText) ? "YEAR TO DATE" : $"YEAR TO DATE ({payslip.YtdPeriodText})";
            var ytd = new Table(UnitValue.CreatePercentArray(new float[] { 62f, 38f })).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(_border, BorderWidth));
            ytd.AddHeaderCell(new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBackgroundColor(_dark).SetPadding(6).SetPaddingLeft(8)
                .Add(Para(ytdTitle, 9f, true, White)));
            ytd.AddHeaderCell(ColumnHeader("Particulars", TextAlignment.LEFT));
            ytd.AddHeaderCell(ColumnHeader($"Amount ({_currencySymbol})", TextAlignment.RIGHT));
            for (var i = 0; i < rowCount; i++)
            {
                var row = i < ytdRows.Count ? ytdRows[i] : (Label: "", Amount: (decimal?)null);
                ytd.AddCell(BodyCell(row.Label, TextAlignment.LEFT));
                ytd.AddCell(BodyCell(row.Amount.HasValue ? FormatAmount(row.Amount.Value) : "", TextAlignment.RIGHT));
            }

            var layout = new Table(UnitValue.CreatePercentArray(new float[] { 55f, 2f, 43f })).UseAllAvailableWidth();
            layout.SetKeepTogether(true);
            layout.AddCell(NoBorderCell().SetPadding(0).Add(contributions));
            layout.AddCell(NoBorderCell());
            layout.AddCell(NoBorderCell().SetPadding(0).Add(ytd));
            return layout;
        }

        private Table CreateFooter(EmployeePayslipDto payslip)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 62f, 38f })).UseAllAvailableWidth();
            table.SetKeepTogether(true);

            var remarks = NoBorderCell().SetVerticalAlignment(VerticalAlignment.BOTTOM);
            remarks.Add(Para("Remarks:", 9.5f, true, Black).SetMarginBottom(2));
            remarks.Add(Para("1.   This is a computer generated salary slip and does not require a signature.", 8.5f, false, Black));
            remarks.Add(Para("2.   For any queries, please contact the HR Department.", 8.5f, false, Black));
            table.AddCell(remarks);

            var signatory = NoBorderCell().SetVerticalAlignment(VerticalAlignment.BOTTOM).SetTextAlignment(TextAlignment.CENTER);
            // Signature: SystemParameters TaxAuthorizedSignatureImage; name: TaxAuthorizedPersonName
            var signature = CreateImage(payslip.AuthorisedSignatureImage, 32f);
            if (signature != null)
            {
                signatory.Add(signature.SetHorizontalAlignment(HorizontalAlignment.CENTER));
            }

            var hasName = !string.IsNullOrWhiteSpace(payslip.AuthorisedSignatoryName);
            var signatoryLine = new Paragraph()
                .SetMargin(0).SetMarginTop(4).SetPaddingTop(4)
                .SetBorderTop(new SolidBorder(_dark, 0.8f))
                .SetTextAlignment(TextAlignment.CENTER);
            if (hasName)
            {
                signatoryLine.Add(new Text(payslip.AuthorisedSignatoryName!.Trim()).SetFont(_bold).SetFontSize(9.5f).SetFontColor(Black));
            }
            else
            {
                signatoryLine.Add(new Text("Authorised Signatory").SetFont(_bold).SetFontSize(10).SetFontColor(Black));
            }
            signatory.Add(signatoryLine);

            if (hasName)
            {
                signatory.Add(Para("Authorised Signatory", 8.5f, false, Black).SetTextAlignment(TextAlignment.CENTER));
            }
            table.AddCell(signatory);

            return table;
        }

        /// <summary>Earnings or Deductions box: title bar, column header, rows (padded to rowCount) and total line.</summary>
        private Table AmountSection(string title, string headColumn, List<EmployeeSalaryDetailsDto> rows, int rowCount, string totalLabel, decimal total)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 17f, 53f, 30f })).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(_border, BorderWidth));

            table.AddHeaderCell(new Cell(1, 3).SetBorder(Border.NO_BORDER).SetBackgroundColor(_dark).SetPadding(6).SetPaddingLeft(8)
                .Add(Para(title, 10.5f, true, White)));
            table.AddHeaderCell(ColumnHeader("Sl. No.", TextAlignment.CENTER));
            table.AddHeaderCell(ColumnHeader(headColumn, TextAlignment.LEFT));
            table.AddHeaderCell(ColumnHeader($"Amount ({_currencySymbol})", TextAlignment.RIGHT));

            for (var i = 0; i < rowCount; i++)
            {
                var row = i < rows.Count ? rows[i] : null;
                table.AddCell(BodyCell(row != null ? (i + 1).ToString() : "", TextAlignment.CENTER));
                table.AddCell(BodyCell(row?.Description, TextAlignment.LEFT));
                table.AddCell(BodyCell(row != null ? FormatAmount(row.AmountG) : "", TextAlignment.RIGHT));
            }

            table.AddCell(new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(_border, BorderWidth))
                .SetBackgroundColor(_light).SetPadding(6).SetPaddingLeft(8)
                .Add(Para(totalLabel, 9.5f, true, Black)));
            table.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(_border, BorderWidth))
                .SetBorderLeft(new SolidBorder(_border, BorderWidth))
                .SetBackgroundColor(_light).SetPadding(6)
                .Add(Para(FormatAmount(total), 10.5f, true, Black).SetTextAlignment(TextAlignment.RIGHT)));

            return table;
        }

        #endregion

        #region Cell helpers

        private void ApplyColorPattern(string? pattern)
        {
            var key = string.IsNullOrWhiteSpace(pattern) ? DefaultColorPattern : pattern.Trim();
            if (!ColorPatterns.TryGetValue(key, out var colors))
            {
                colors = ColorPatterns[DefaultColorPattern];
            }

            _dark = colors.Dark;
            _light = colors.Light;
            _pale = colors.Pale;
            _border = colors.Border;
        }

        private Paragraph Para(string? text, float size, bool bold, Color color)
        {
            return new Paragraph(text ?? string.Empty)
                .SetMargin(0)
                .SetFixedLeading(size * 1.25f)
                .SetFont(bold ? _bold : _regular)
                .SetFontSize(size)
                .SetFontColor(color);
        }

        private static Paragraph Spacer(float height) => new Paragraph().SetMargin(0).SetHeight(height);

        private static Cell NoBorderCell() => new Cell().SetBorder(Border.NO_BORDER).SetPadding(0);

        private Cell SectionHeaderLight(string title, int colspan)
        {
            return new Cell(1, colspan)
                .SetBorder(Border.NO_BORDER)
                .SetBorderBottom(new SolidBorder(_border, BorderWidth))
                .SetBackgroundColor(_light)
                .SetPadding(6).SetPaddingLeft(10)
                .Add(Para(title, 10, true, Black));
        }

        private Cell DetailCell(string? text)
        {
            return new Cell()
                .SetBorder(new SolidBorder(_border, BorderWidth))
                .SetPaddingTop(2).SetPaddingBottom(2).SetPaddingLeft(9).SetPaddingRight(4)
                .SetMinHeight(14)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(Para(text, FontSize, false, Black));
        }

        private Cell ColumnHeader(string text, TextAlignment alignment)
        {
            return new Cell()
                .SetBorder(new SolidBorder(_border, BorderWidth))
                .SetBackgroundColor(_light)
                .SetPaddingTop(3).SetPaddingBottom(3).SetPaddingLeft(5).SetPaddingRight(6)
                .Add(Para(text, FontSize, true, Black).SetTextAlignment(alignment));
        }

        private Cell BodyCell(string? text, TextAlignment alignment)
        {
            return new Cell()
                .SetBorder(new SolidBorder(_border, BorderWidth))
                .SetPaddingTop(2).SetPaddingBottom(2).SetPaddingLeft(5).SetPaddingRight(6)
                .SetMinHeight(14)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(Para(text, FontSize, false, Black).SetTextAlignment(alignment));
        }

        private static Image? CreateImage(byte[]? bytes, float height)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            try
            {
                var image = new Image(ImageDataFactory.Create(bytes));
                var width = image.GetImageWidth() * height / image.GetImageHeight();
                return image.ScaleAbsolute(width, height);
            }
            catch
            {
                return null; // Unsupported image data: leave it out rather than fail the payslip
            }
        }

        #endregion

        #region Formatting

        /// <summary>Indian grouping, e.g. 1,00,000.00</summary>
        private static string FormatAmount(decimal amount) => amount.ToString("N2", IndianCulture);

        /// <summary>e.g. "Rupees Eighty Five Thousand Two Hundred Seventy Five Only" (Indian system: lakh, crore).</summary>
        public static string AmountInWords(decimal amount)
        {
            var negative = amount < 0;
            amount = Math.Abs(Math.Round(amount, 2, MidpointRounding.AwayFromZero));
            var rupees = (long)Math.Floor(amount);
            var paise = (int)((amount - rupees) * 100);

            var words = "Rupees " + (rupees == 0 ? "Zero" : NumberToWords(rupees));
            if (paise > 0)
            {
                words += " and " + NumberToWords(paise) + " Paise";
            }
            return (negative ? "Minus " : "") + words + " Only";
        }

        private static readonly string[] Units =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };
        private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        private static string NumberToWords(long number)
        {
            var parts = new List<string>();
            void Add(long value, string suffix)
            {
                if (value > 0) parts.Add(BelowHundredWords((int)value) + (suffix.Length > 0 ? " " + suffix : ""));
            }

            var crore = number / 10000000;
            number %= 10000000;
            if (crore > 0) parts.Add(NumberToWords(crore) + " Crore");
            Add(number / 100000, "Lakh"); number %= 100000;
            Add(number / 1000, "Thousand"); number %= 1000;
            Add(number / 100, "Hundred"); number %= 100;
            if (number > 0) parts.Add(BelowHundredWords((int)number));

            return string.Join(" ", parts);
        }

        private static string BelowHundredWords(int number)
        {
            if (number < 20) return Units[number];
            return Tens[number / 10] + (number % 10 > 0 ? " " + Units[number % 10] : "");
        }

        #endregion
    }
}
