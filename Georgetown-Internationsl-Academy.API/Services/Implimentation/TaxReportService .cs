using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO.Compression;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class TaxReportService : ITaxReportService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<TaxReportService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public TaxReportService(
            ApplicationDBContext dbContext,
            ILogger<TaxReportService> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public async Task<byte[]> GenerateTaxReportsAsync(List<int> employeeIds, int financialYear)
        {
            if (employeeIds == null || !employeeIds.Any())
                throw new ArgumentException("No employee IDs provided.");

            try
            {
                using var connection = _dbContext.Database.GetDbConnection();

                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync();

                string templatePath = @"C:\Users\User\Downloads\G0024-7B-FormTemplate.pdf";

                // If single employee, return a single PDF (not a ZIP)
                if (employeeIds.Count == 1)
                {
                    var empId = employeeIds[0];
                    _logger.LogInformation("Generating single 7B Tax Report for Employee ID: {EmpId}, Financial Year: {FinancialYear}", empId, financialYear);

                    var result = await connection.QueryMultipleAsync(
                        "GetData_7BtaxReport",
                        new { IdEmployee = empId, IdFinancialYear = financialYear },
                        commandType: CommandType.StoredProcedure);

                    var empData = await result.ReadFirstOrDefaultAsync<TaxReportDto>();
                    if (empData == null)
                    {
                        _logger.LogWarning("No employee data found for Employee ID {EmpId}", empId);
                        return Array.Empty<byte>();
                    }
                    empData.SalaryHeadDetails = (await result.ReadAsync<TaxReportDetailDto>()).ToList();

                    return GeneratePdfFromTemplate(empData, templatePath);
                }

                // Prepare ZIP stream for multiple employees
                using var memoryStream = new MemoryStream();
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var empId in employeeIds)
                {
                    _logger.LogInformation("Generating 7B Tax Report for Employee ID: {EmpId}, Financial Year: {FinancialYear}", empId, financialYear);

                    // Call stored procedure
                    var result = await connection.QueryMultipleAsync(
     "GetData_7BtaxReport",
     new { IdEmployee = empId, IdFinancialYear = financialYear },
     commandType: CommandType.StoredProcedure);

                    var empData = await result.ReadFirstOrDefaultAsync<TaxReportDto>();
                    if (empData == null)
                    {
                        _logger.LogWarning("No employee data found for Employee ID {EmpId}", empId);
                        continue;
                    }
                    empData.SalaryHeadDetails = (await result.ReadAsync<TaxReportDetailDto>()).ToList();

                    // Generate PDF file
                    byte[] pdfBytes = GeneratePdfFromTemplate(empData, templatePath);

                    // Add PDF to ZIP archive
                    var entry = zipArchive.CreateEntry($"Form7B_TaxReport_Emp{empId}_{financialYear}.pdf");
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                    }
                }

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating 7B Tax Reports for Financial Year {FinancialYear}", financialYear);
                throw;
            }
        }
        private byte[] GeneratePdfFromTemplate(TaxReportDto data, string templatePath)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("PDF template not found.", templatePath);

            using var memoryStream = new MemoryStream();
            using (var reader = new PdfReader(templatePath))
            using (var stamper = new PdfStamper(reader, memoryStream, '\0', false)) // appendMode = false
            {
                var formFields = stamper.AcroFields;

                // Ensure appearance generation for filled fields (prevents some viewers from showing blanks)
                formFields.GenerateAppearances = true;

                // Prevent iTextSharp from closing the underlying MemoryStream when stamper.Close() is called
                if (stamper.Writer != null)
                {
                    stamper.Writer.CloseStream = false;
                }

                // Debug: log all available field names for mapping
                try
                {
                    LogPdfFormFields(formFields);
                }
                catch { }

                // Fill form fields as before
                formFields.SetField("EmployeeNameField", data.EmployeeName ?? "");
                formFields.SetField("EmployeeNumberField", data.EmployeeNumber ?? "");

                if (data.SalaryHeadDetails != null)
                {
                    for (int i = 0; i < data.SalaryHeadDetails.Count; i++)
                    {
                        var item = data.SalaryHeadDetails[i];
                        formFields.SetField($"SalaryHeadName{i + 1}", item.SalaryHeadName ?? "");
                        formFields.SetField($"YTDAmount{i + 1}", item.YTDAmount.ToString("N2"));
                    }
                }

                // Default placeholder when value missing (as requested)
                const string placeholder = "123";

                // Employer details
                TrySetField(formFields, new[] { "TIN[0]", "EmployerTIN[0]" }, string.IsNullOrWhiteSpace(data.EmployerTIN) ? placeholder : data.EmployerTIN);
                TrySetField(formFields, new[] { "TIN[1]", "EmployerTIN[1]" }, string.IsNullOrWhiteSpace(data.EmployerTIN) ? placeholder : data.EmployerTIN);
                TrySetField(formFields, new[] { "TextField5[0]", "TextField5[5]", "EmployerNameField" }, string.IsNullOrWhiteSpace(data.EmployerName) ? placeholder : data.EmployerName);
                TrySetField(formFields, new[] { "TextField5[1]", "TextField5[4]", "EmployerAddressField" }, string.IsNullOrWhiteSpace(data.EmployerAddress) ? placeholder : data.EmployerAddress);

                // Employee details
                TrySetField(formFields, new[] { "TextField5[2]", "TextField5[6]", "EmployeeNameField" }, string.IsNullOrWhiteSpace(data.EmployeeName) ? placeholder : data.EmployeeName);
                TrySetField(formFields, new[] { "TextField5[3]", "TextField5[7]", "EmployeeAddressField" }, string.IsNullOrWhiteSpace(data.EmployeeAddress) ? placeholder : data.EmployeeAddress);
                TrySetField(formFields, new[] { "TINOriginal[0]", "EmployeeTIN[0]" }, string.IsNullOrWhiteSpace(data.EmployeeTIN) ? placeholder : data.EmployeeTIN);
                TrySetField(formFields, new[] { "TINOriginal[1]", "EmployeeTIN[1]" }, string.IsNullOrWhiteSpace(data.EmployeeTIN) ? placeholder : data.EmployeeTIN);
                TrySetField(formFields, new[] { "RegOriginal[0]", "NISOriginal[0]" }, string.IsNullOrWhiteSpace(data.NISNumber) ? placeholder : data.NISNumber);
                TrySetField(formFields, new[] { "RegOriginal[1]", "NISOriginal[1]" }, string.IsNullOrWhiteSpace(data.NISNumber) ? placeholder : data.NISNumber);
                TrySetField(formFields, new[] { "SeqOriginal[0]", "EmployeeNumberField" }, string.IsNullOrWhiteSpace(data.EmployeeNumber) ? placeholder : data.EmployeeNumber);
                TrySetField(formFields, new[] { "SeqOriginal[1]", "EmployeeNumberField2" }, string.IsNullOrWhiteSpace(data.EmployeeNumber) ? placeholder : data.EmployeeNumber);

                // Period dates (yyyyMMdd as in WinForms sample)
                var periodFrom = !data.PeriodFrom.HasValue || data.PeriodFrom.Value == default ? placeholder : data.PeriodFrom.Value.ToString("yyyyMMdd");
                var periodTo = !data.PeriodTo.HasValue || data.PeriodTo.Value == default ? placeholder : data.PeriodTo.Value.ToString("yyyyMMdd");
                TrySetField(formFields, new[] { "NumericField87[0]", "PeriodFrom1" }, periodFrom);
                TrySetField(formFields, new[] { "NumericField87[2]", "PeriodFrom2" }, periodFrom);
                TrySetField(formFields, new[] { "NumericField87[1]", "PeriodTo1" }, periodTo);
                TrySetField(formFields, new[] { "NumericField87[3]", "PeriodTo2" }, periodTo);

                // Earnings and allowances
                TrySetField(formFields, new[] { "NumericField1[0]" }, (data.SalaryOrWages ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField1[1]" }, (data.SalaryOrWages ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField3[0]", "NumericField3[3]" }, (data.RentFreeQuartersOrHouseAllowance ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField4[0]", "NumericField4[1]" }, (data.BonusAndProfitShare ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField5[0]", "NumericField5[1]" }, (data.Overtime ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField6[0]", "NumericField6[4]" }, (data.BoardAndLodge ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField6[1]", "NumericField6[5]" }, (data.Fees ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField3[1]", "NumericField3[4]" }, (data.OtherAllowances ?? 0m).ToString("N2"));

                // Non-taxable allowances (max 3 as per sample)
                if (data.SalaryHeadDetails != null)
                {
                    if (data.SalaryHeadDetails.Count > 0)
                    {
                        TrySetField(formFields, new[] { "TextField7[0]", "TextField7[1]" }, data.SalaryHeadDetails[0].SalaryHeadName ?? placeholder);
                        TrySetField(formFields, new[] { "NumericField6[2]", "NumericField6[6]" }, (data.SalaryHeadDetails[0].YTDAmount).ToString("N2"));
                    }
                    if (data.SalaryHeadDetails.Count > 1)
                    {
                        TrySetField(formFields, new[] { "TextField6[0]", "TextField6[1]" }, data.SalaryHeadDetails[1].SalaryHeadName ?? placeholder);
                        TrySetField(formFields, new[] { "NumericField6[3]", "NumericField6[7]" }, (data.SalaryHeadDetails[1].YTDAmount).ToString("N2"));
                    }
                    if (data.SalaryHeadDetails.Count > 2)
                    {
                        TrySetField(formFields, new[] { "TextField8[0]", "TextField8[1]" }, data.SalaryHeadDetails[2].SalaryHeadName ?? placeholder);
                        TrySetField(formFields, new[] { "NumericField3[2]", "NumericField3[5]" }, (data.SalaryHeadDetails[2].YTDAmount).ToString("N2"));
                    }
                }

                // Totals and deductions
                TrySetField(formFields, new[] { "NumericField8[0]", "NumericField8[5]", "TotalIncomeField", "TotalIncome[0]", "TotalIncome[1]" }, (data.TotalIncome ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField8[1]", "NumericField8[6]", "TotalTaxableIncomeField", "TotalTaxableIncome[0]", "TotalTaxableIncome[1]" }, (data.TotalTaxableIncome ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField8[2]", "NumericField8[7]", "NISContributionField", "NIS[0]", "NIS[1]" }, (data.NISContribution ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField8[3]", "NumericField8[8]", "MedicalLifeInsuranceField", "MedicalAndLifeInsurancePremiums[0]", "MedicalAndLifeInsurancePremiums[1]" }, (data.MedicalAndLifeInsurancePremiums ?? 0m).ToString("N2"));
                TrySetField(formFields, new[] { "NumericField8[4]", "NumericField8[9]", "IncomeTaxDeductedField", "IncomeTaxDeducted[0]", "IncomeTaxDeducted[1]", "IncomeTaxDeducted2" }, (data.IncomeTaxDeducted ?? 0m).ToString("N2"));

                // Add signature
                int page = 1;
                PdfContentByte content = stamper.GetOverContent(page);
                string signaturePath = @"C:\Users\User\Pictures\Screenshots\Signature1.jpg.png";
                if (File.Exists(signaturePath))
                {
                    var signature = iTextSharp.text.Image.GetInstance(signaturePath);
                    signature.ScaleToFit(150f, 30f);
                    // Left panel signature
                    signature.SetAbsolutePosition(220, 30);
                    content.AddImage(signature);

                    // Right panel signature (duplicate side)
                    var signatureRight = iTextSharp.text.Image.GetInstance(signaturePath);
                    signatureRight.ScaleToFit(150f, 30f);
                    signatureRight.SetAbsolutePosition(520, 30);
                    content.AddImage(signatureRight);
                }

                // Mirror any left-side field values to right-side fields when
                // the template uses indexed names like FieldName[0] and FieldName[1].
                // This covers any fields we didn't explicitly map above.
                try { MirrorRightSideFields(formFields); } catch (Exception ex) { _logger.LogDebug(ex, "Failed while mirroring right-side PDF fields."); }
                try { MirrorAllIndexedFields(formFields); } catch (Exception ex) { _logger.LogDebug(ex, "Failed while broadcasting values across indexed fields."); }

                stamper.FormFlattening = true; // works now
                stamper.Close();
            }

            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }

        private void LogPdfFormFields(AcroFields formFields)
        {
            try
            {
                var allFieldNames = formFields.Fields?.Keys;
                if (allFieldNames == null || !allFieldNames.Any())
                {
                    _logger.LogWarning("No AcroForm fields found in the provided PDF template.");
                    return;
                }

                foreach (var name in allFieldNames)
                {
                    var item = formFields.GetFieldItem(name);
                    var fieldType = formFields.GetFieldType(name);
                    _logger.LogInformation("PDF Field: {FieldName}, Type: {FieldType}, Value: {Value}", name, fieldType, formFields.GetField(name));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging PDF form fields.");
            }
        }

        private void TrySetField(AcroFields formFields, IEnumerable<string> candidateNames, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            foreach (var name in candidateNames)
            {
                try
                {
                    if (formFields.GetFieldItem(name) != null)
                    {
                        var success = formFields.SetField(name, value);
                        if (success)
                        {
                            _logger.LogDebug("Set PDF field {FieldName} successfully.", name);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed setting PDF field {FieldName}.", name);
                }
            }
        }

        // Mirrors values from fields ending with [0] to corresponding [1] fields, if present.
        private void MirrorRightSideFields(AcroFields formFields)
        {
            var keys = formFields.Fields?.Keys;
            if (keys == null) return;

            foreach (var name in keys)
            {
                if (!name.EndsWith("[0]", StringComparison.Ordinal)) continue;

                var baseName = name.Substring(0, name.Length - 3);
                var rightName = baseName + "[1]";

                if (formFields.GetFieldItem(rightName) == null) continue;

                var leftValue = formFields.GetField(name);
                if (leftValue == null) continue;

                try
                {
                    formFields.SetField(rightName, leftValue);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed mirroring field {Left} -> {Right}", name, rightName);
                }
            }
        }

        // For any field that follows the pattern Name[index], copy its value to any other
        // existing indexes (0..9) that are empty, to cover duplicate-panel templates.
        private void MirrorAllIndexedFields(AcroFields formFields)
        {
            var keys = formFields.Fields?.Keys;
            if (keys == null) return;

            // snapshot values to avoid mutation during iteration
            var fieldValues = new Dictionary<string, string?>();
            foreach (var key in keys)
            {
                fieldValues[key] = formFields.GetField(key);
            }

            foreach (var kvp in fieldValues)
            {
                var name = kvp.Key;
                var value = kvp.Value;
                if (string.IsNullOrEmpty(value)) continue;

                // looks like Pattern[digits]
                var openBracket = name.LastIndexOf('[');
                var closeBracket = name.LastIndexOf(']');
                if (openBracket < 0 || closeBracket < 0 || closeBracket <= openBracket + 1) continue;

                var indexPart = name.Substring(openBracket + 1, closeBracket - openBracket - 1);
                if (!int.TryParse(indexPart, out var _)) continue;

                var baseName = name.Substring(0, openBracket);
                for (int i = 0; i <= 9; i++)
                {
                    var candidate = baseName + "[" + i + "]";
                    if (candidate == name) continue;
                    if (formFields.GetFieldItem(candidate) == null) continue;

                    var existing = formFields.GetField(candidate);
                    if (!string.IsNullOrWhiteSpace(existing)) continue;

                    try { formFields.SetField(candidate, value); }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed broadcasting field {From} -> {To}", name, candidate);
                    }
                }
            }
        }




    }
}
