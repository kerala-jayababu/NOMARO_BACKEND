using Georgetown_Internationsl_Academy.API.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font;
using iText.Layout.Properties;
using DinkToPdf;
using DinkToPdf.Contracts;
using iText.IO.Font.Constants;
using Document = iText.Layout.Document;
using Table = iText.Layout.Element.Table;
using Serilog;
using iText.Kernel.Geom;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class PaySlipGeneratorDto
    {
        private TextAlignment alignCenter;
        private iText.Layout.Properties.TextAlignment alignLeft;
        private iText.Layout.Properties.TextAlignment alignRight;

        private iText.Kernel.Colors.Color bgGray;
        private iText.Kernel.Colors.Color bgYellow;
        private iText.Kernel.Colors.Color bgWhite;
        private iText.Kernel.Colors.Color bgNetPay;
        private iText.Kernel.Colors.Color lineColor;
        int rowHeight = 14;

        public void GeneratePayslipPdf(EmployeePayslipDto payslip, Stream outputStream)
        {
            SetDefaultValues();

            using (PdfWriter writer = new PdfWriter(outputStream))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document document = new Document(pdf))
            {
                pdf.SetDefaultPageSize(PageSize.LETTER);
                AddLogo(document,payslip.logo,payslip.logoType);
                PdfFont boldFont1 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                document.Add(new Paragraph("Payslip").SetFontSize(18).SetTextAlignment(alignRight).SetFont(boldFont1));
                document.Add(new Paragraph($"Payslip for the period: {payslip.Period}\n").SetTextAlignment(alignRight));

                document.Add(CreateEmployeeDetails(document, payslip));
                document.Add(new Paragraph("").SetHeight(6));

                // Earnings Table
                document.Add(CreateSalaryTable(payslip.Earnings, "Earnings"));
                document.Add(new Paragraph("").SetHeight(6));
                document.Add(CreateSalaryTable(payslip.Deductions, "Deductions"));

                //// Income Tax Details
                //document.Add(new Paragraph(""));
                //document.Add(CreateSalaryTable(payslip.TaxDetails, "Income Tax Deductions"));

                // Calculate totals for all columns
                decimal totalEarningsG = payslip.Earnings.Sum(x => x.AmountG);
                decimal totalEarningsUS = payslip.Earnings.Sum(x => x.AmountUS);
                decimal totalEarningsYTDG = payslip.Earnings.Sum(x => x.YTDAmountG);
                decimal totalEarningsYTDUSD = payslip.Earnings.Sum(x => x.YTDAmountUSD);

                decimal totalDeductionsG = payslip.Deductions.Sum(x => x.AmountG);
                decimal totalDeductionsUS = payslip.Deductions.Sum(x => x.AmountUS);
                decimal totalDeductionsYTDG = payslip.Deductions.Sum(x => x.YTDAmountG);
                decimal totalDeductionsYTDUSD = payslip.Deductions.Sum(x => x.YTDAmountUSD);

                // Calculate net amounts (earnings - deductions)
                decimal netPayG = totalEarningsG - totalDeductionsG;
                decimal netPayUS = totalEarningsUS - totalDeductionsUS;
                decimal netPayYTDG = totalEarningsYTDG - totalDeductionsYTDG;
                decimal netPayYTDUSD = totalEarningsYTDUSD - totalDeductionsYTDUSD;

                // Add Net Pay table with all 4 columns
                document.Add(new Paragraph("").SetHeight(6));
                document.Add(CreateNetPayTable(netPayG, netPayUS, netPayYTDG, netPayYTDUSD));
                document.Add(new Paragraph("").SetHeight(6));
                AddStamp(document,payslip.stamp,payslip.stampType);
                document.Add(new Paragraph($"Payslip generated on: {payslip.PayslipGeneratedDate}").SetFontSize(9));
                document.Close();
            }
        }

        private void SetDefaultValues()
        {
            alignLeft = TextAlignment.LEFT;
            alignRight = TextAlignment.RIGHT;
            alignCenter = TextAlignment.CENTER;

            bgGray = new iText.Kernel.Colors.DeviceRgb(240, 240, 240);
            bgYellow = new iText.Kernel.Colors.DeviceRgb(255, 255, 235);
            bgWhite = new iText.Kernel.Colors.DeviceRgb(255, 255, 255);
            bgNetPay = new iText.Kernel.Colors.DeviceRgb(155, 178, 185);
            lineColor = new iText.Kernel.Colors.DeviceRgb(210, 210, 210);
        }

        private void AddLogo(Document document,byte[] logo,string logotype)
        {
            //byte[] fileBytes = ConvertHexStringToByteArray(logo);
            iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(logo));
            //iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(@"C:\Sandeep\Logo.png"));
            iText.Kernel.Geom.Rectangle pageSize = document.GetPdfDocument().GetDefaultPageSize();
            float leftMargin = 30;
            float topMargin = 20;

            // Position logo at top-left with margin
            float x = leftMargin;
            float y = pageSize.GetHeight() - img.GetImageHeight() - topMargin;

            img.SetFixedPosition(x, y);
            document.Add(img);
        }

        static byte[] ConvertHexStringToByteArray(string hexString)
        {
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        private void AddStamp(Document document, byte[] stamp, string stamptype)
        {
            //iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(@"C:\Sandeep\GIASeal.png"));
            iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(stamp));
            float y = document.GetRenderer().GetCurrentArea().GetBBox().GetY();
            float x = document.GetPdfDocument().GetDefaultPageSize().GetWidth() - img.GetImageScaledWidth() - 30;
            img.SetFixedPosition(x, y-10);
            document.Add(img);
        }
        private Table CreateEmployeeDetails(Document document, EmployeePayslipDto payslip)
        {
            float[] columnWidths = { 16f, 34f, 16f, 34f };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            // Row 1
            table.AddCell(CreateStyledCell("Emp Code", lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(payslip.EmployeeCode, lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell("Emp Name", lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(payslip.EmployeeName, lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));

            // Row 2
            table.AddCell(CreateStyledCell("Designation", lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(payslip.Position, lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell("Department", lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(payslip.Department, lineColor, 1, bgWhite, false, alignLeft).SetHeight(rowHeight));

            return table;
        }


        Table CreateSalaryTable(List<EmployeeSalaryDetailsDto> details, String Heading)
        {
            float[] columnWidths = { 30f, 17.5f, 17.5f, 17.5f, 17.5f };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();
            float borderWidth = 0.5f;
            table.AddHeaderCell(CreateStyledCell(Heading, lineColor, borderWidth, bgGray, true, alignLeft).SetHeight(rowHeight));
            table.AddHeaderCell(CreateStyledCell("Amount(G$)", lineColor, borderWidth, bgGray, false, alignCenter).SetHeight(rowHeight));
            table.AddHeaderCell(CreateStyledCell("Amount(US$)", lineColor, borderWidth, bgGray, false, alignCenter).SetHeight(rowHeight));
            table.AddHeaderCell(CreateStyledCell("YTDAmount(G$)", lineColor, borderWidth, bgGray, false, alignCenter).SetHeight(rowHeight));
            table.AddHeaderCell(CreateStyledCell("YTDAmount(US$)", lineColor, borderWidth, bgGray, false, alignCenter).SetHeight(rowHeight));



            foreach (var detail in details)
            {
                table.AddCell(CreateStyledCell(detail.Description, lineColor, borderWidth, bgWhite, false, alignLeft).SetHeight(rowHeight));
                table.AddCell(CreateStyledCell(detail.AmountG.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight).SetHeight(rowHeight));
                table.AddCell(CreateStyledCell(detail.AmountUS.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight).SetHeight(rowHeight));
                table.AddCell(CreateStyledCell(detail.YTDAmountG.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight).SetHeight(rowHeight));
                table.AddCell(CreateStyledCell(detail.YTDAmountUSD.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight).SetHeight(rowHeight));

            }

            decimal totalG = details.Sum(x => x.AmountG);
            decimal totalUS = details.Sum(x => x.AmountUS);
            decimal totalYTDG = details.Sum(x => x.YTDAmountG);
            decimal totalYTDUSD = details.Sum(x => x.YTDAmountUSD);

            // Add Totals Row
            table.AddCell(CreateStyledCell($"Total", lineColor, borderWidth, bgYellow, true, alignCenter).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(totalG.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(totalUS.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(totalYTDG.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(totalYTDUSD.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));

            return table;
        }

        Table CreateNetPayTable(decimal netPayG, decimal netPayUS, decimal netPayYTDG, decimal netPayYTDUSD)
        {
            float[] columnWidths = { 30f, 17.5f, 17.5f, 17.5f, 17.5f };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();
            float borderWidth = 0.5f;

            // Single row with Net Pay label and values
            table.AddCell(CreateStyledCell("Net Pay", lineColor, borderWidth, bgYellow, true, alignCenter).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(netPayG.ToString("N2"), lineColor, borderWidth, bgNetPay, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(netPayUS.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(netPayYTDG.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));
            table.AddCell(CreateStyledCell(netPayYTDUSD.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight).SetHeight(rowHeight));

            return table;
        }

        Cell CreateStyledCell(string text, iText.Kernel.Colors.Color borderColor, float borderWidth,
                       iText.Kernel.Colors.Color backgroundColor, bool isBold,
                       iText.Layout.Properties.TextAlignment alignment)
        {
            Paragraph paragraph = new Paragraph(text)
                .SetTextAlignment(alignment).SetFontSize(10);

            if (isBold)
            {
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                paragraph.SetFont(boldFont);
            }
            else
            {
                PdfFont nFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                paragraph.SetFont(nFont);
            }

            return new Cell()
                .Add(paragraph)
                .SetFontSize(12)
                .SetHeight(17)
                .SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, (float)0.5))
                .SetBackgroundColor(backgroundColor)
                .SetPadding(5)
                .SetTextAlignment(alignment);

        }

        Cell CreateStyledCellNetSalary(string text, iText.Kernel.Colors.Color borderColor, float borderWidth,
               iText.Kernel.Colors.Color backgroundColor, bool isBold,
               iText.Layout.Properties.TextAlignment alignment)
        {
            Paragraph paragraph = new Paragraph(text)
                .SetTextAlignment(alignment).SetFontSize(12);

            if (isBold)
            {
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                paragraph.SetFont(boldFont);
            }
            else
            {
                PdfFont nFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                paragraph.SetFont(nFont);
            }

            return new Cell()
                .Add(paragraph)
                .SetFontSize(12)
                .SetHeight(17)
                .SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, (float)0.5))
                .SetBackgroundColor(backgroundColor)
                .SetPadding(5)
                .SetTextAlignment(alignment);

        }


    }
}
