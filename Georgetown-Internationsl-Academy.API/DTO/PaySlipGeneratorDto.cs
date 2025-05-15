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
        private iText.Kernel.Colors.Color lineColor;

        public void GeneratePayslipPdf(EmployeePayslipDto payslip, Stream outputStream)
        {
            SetDefaultValues();

            using (PdfWriter writer = new PdfWriter(outputStream))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document document = new Document(pdf))
            {
              
                AddLogo(document,payslip.logo,payslip.logoType);
                PdfFont boldFont1 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                document.Add(new Paragraph("Payslip").SetFontSize(18).SetTextAlignment(alignLeft).SetFont(boldFont1));
                document.Add(new Paragraph($"Payslip for the period: {payslip.Period}\n\n"));

                document.Add(CreateEmployeeDetails(document, payslip));
                document.Add(new Paragraph(""));

                // Earnings Table
                document.Add(CreateSalaryTable(payslip.Earnings, "Earnings"));
                document.Add(new Paragraph(""));

                // Deductions Table
                document.Add(new Paragraph(""));
                document.Add(CreateSalaryTable(payslip.Deductions, "Deductions"));

                //// Income Tax Details
                //document.Add(new Paragraph(""));
                //document.Add(CreateSalaryTable(payslip.TaxDetails, "Income Tax Deductions"));

                // Net Pay
                decimal totalEarnings = payslip.Earnings.Sum(x => x.AmountG);
                decimal totalDeductions = payslip.Deductions.Sum(x => x.AmountG);
                decimal netPay = totalEarnings - totalDeductions;
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                document.Add(new Paragraph($"\nNet Pay G$ {netPay.ToString("N2")}\n").SetFont(boldFont).SetTextAlignment(alignCenter));
                AddStamp(document,payslip.stamp,payslip.stampType);
                document.Add(new Paragraph($"Payslip generated on: {payslip.PayslipGeneratedDate}"));
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
            lineColor = new iText.Kernel.Colors.DeviceRgb(210, 210, 210);
        }

        private void AddLogo(Document document,byte[] logo,string logotype)
        {
            //byte[] fileBytes = ConvertHexStringToByteArray(logo);
            iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(logo));
            //iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(@"C:\Sandeep\Logo.png"));
            iText.Kernel.Geom.Rectangle pageSize = document.GetPdfDocument().GetDefaultPageSize();
            float x = pageSize.GetWidth() - img.GetImageWidth() - 30; // 20 is the right margin
            float y = pageSize.GetHeight() - img.GetImageHeight() - 20; // 20 is the top margin

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
            img.SetFixedPosition(x, y);
            document.Add(img);
        }
        private Table CreateEmployeeDetails(Document document, EmployeePayslipDto payslip)
        {
            float[] columnWidths = { 16f, 34f, 16f, 34f };

            Table table = new Table(UnitValue.CreatePercentArray(columnWidths));
            table.SetWidth(UnitValue.CreatePercentValue(100));
            // Row 1
            table.AddCell(CreateStyledCell("Emp Code", lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell(payslip.EmployeeCode, lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell("Emp Name", lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell(payslip.EmployeeName, lineColor, 1, bgWhite, false, alignLeft));

            // Row 2
            table.AddCell(CreateStyledCell("Designation", lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell(payslip.Position, lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell("Department", lineColor, 1, bgWhite, false, alignLeft));
            table.AddCell(CreateStyledCell(payslip.Department, lineColor, 1, bgWhite, false, alignLeft));

            return table;
        }


        Table CreateSalaryTable(List<EmployeeSalaryDetailsDto> details, String Heading)
        {
            float[] columnWidths = { 30f, 17.5f, 17.5f, 17.5f, 17.5f };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();
            float borderWidth = 0.5f;
            table.AddHeaderCell(CreateStyledCell(Heading, lineColor, borderWidth, bgGray, true, alignLeft));
            table.AddHeaderCell(CreateStyledCell("Amount(G$)", lineColor, borderWidth, bgGray, false, alignCenter));
            table.AddHeaderCell(CreateStyledCell("Amount(US$)", lineColor, borderWidth, bgGray, false, alignCenter));
            table.AddHeaderCell(CreateStyledCell("YTDAmount(G$)", lineColor, borderWidth, bgGray, false, alignCenter));
            table.AddHeaderCell(CreateStyledCell("YTDAmount(US$)", lineColor, borderWidth, bgGray, false, alignCenter));


            foreach (var detail in details)
            {
                table.AddCell(CreateStyledCell(detail.Description, lineColor, borderWidth, bgWhite, false, alignLeft));
                table.AddCell(CreateStyledCell(detail.AmountG.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight));
                table.AddCell(CreateStyledCell(detail.AmountUS.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight));
                table.AddCell(CreateStyledCell(detail.YTDAmountG.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight));
                table.AddCell(CreateStyledCell(detail.YTDAmountUSD.ToString("N2"), lineColor, borderWidth, bgWhite, false, alignRight));

            }

            decimal totalG = details.Sum(x => x.AmountG);
            decimal totalUS = details.Sum(x => x.AmountUS);
            decimal totalYTDG = details.Sum(x => x.YTDAmountG);
            decimal totalYTDUSD = details.Sum(x => x.YTDAmountUSD);

            // Add Totals Row
            table.AddCell(CreateStyledCell($"Total", lineColor, borderWidth, bgYellow, true, alignCenter));
            table.AddCell(CreateStyledCell(totalG.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight));
            table.AddCell(CreateStyledCell(totalUS.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight));
            table.AddCell(CreateStyledCell(totalYTDG.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight));
            table.AddCell(CreateStyledCell(totalYTDUSD.ToString("N2"), lineColor, borderWidth, bgYellow, true, alignRight));

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

    }
}
