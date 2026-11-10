using System.Drawing;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.DTO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Document Management reports. Each report returns columns + rows, shown on screen and exported to Excel from the same data.
    ///   EXPIRY      - current documents expired / expiring up to a date
    ///   MISSING     - documents never uploaded, and periods missing for periodic documents
    ///   COMPLIANCE  - status counts and compliance % per office (or per type)
    ///   ACTIVITY    - audit trail entries in a date range
    /// Office access is the same as the register.
    /// </summary>
    public partial class GeneralDocumentService
    {
        private const int MaxReportRows = 10000;

        public static readonly (string Code, string Name)[] Reports =
        {
            ("EXPIRY", "Expiry / Renewal Report"),
            ("MISSING", "Missing / Pending Documents"),
            ("COMPLIANCE", "Office Compliance Summary"),
            ("ACTIVITY", "Upload & Activity Log"),
        };

        private static readonly Dictionary<string, string> StatusText = new()
        {
            ["VALID"] = "Valid",
            ["NO_EXPIRY"] = "No expiry",
            ["EXPIRING"] = "Expiring",
            ["EXPIRED"] = "Expired",
            ["NO_RECORD"] = "Not uploaded",
        };

        private static readonly Dictionary<string, string> ActionText = new()
        {
            ["CREATE"] = "Registered",
            ["UPDATE"] = "Details changed",
            ["ACTIVATE"] = "Activated",
            ["DEACTIVATE"] = "Deactivated",
            ["UPLOAD"] = "Uploaded",
            ["RECORD_UPDATE"] = "Record corrected",
            ["MARK_INCORRECT"] = "Marked incorrect",
            ["MARK_SUPERSEDED"] = "Marked superseded",
            ["VIEW"] = "Viewed",
            ["DOWNLOAD"] = "Downloaded",
            ["ZIP_DOWNLOAD"] = "Downloaded (ZIP)",
        };

        public async Task<GeneralDocumentReportDto> GetReport(GeneralDocumentReportFilterDto filter, GeneralDocumentUserContext user)
        {
            if (filter == null || string.IsNullOrWhiteSpace(filter.ReportCode)) throw new InvalidOperationException("Select the report.");
            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.ToDate.Value.Date < filter.FromDate.Value.Date)
                throw new InvalidOperationException("To date cannot be before From date.");

            var report = filter.ReportCode.Trim().ToUpperInvariant() switch
            {
                "EXPIRY" => await ExpiryReport(filter, user),
                "MISSING" => await MissingReport(filter, user),
                "COMPLIANCE" => await ComplianceReport(filter, user),
                "ACTIVITY" => await ActivityReport(filter, user),
                _ => throw new InvalidOperationException("Select a valid report.")
            };
            report.ReportCode = filter.ReportCode.Trim().ToUpperInvariant();
            report.Title = Reports.First(r => r.Code == report.ReportCode).Name;
            report.Criteria = await CriteriaText(filter);
            if (report.Rows.Count > MaxReportRows)
            {
                report.Rows = report.Rows.Take(MaxReportRows).ToList();
                report.Truncated = true;
            }
            return report;
        }

        private GeneralDocumentFilterDto DocumentFilter(GeneralDocumentReportFilterDto f) => new()
        {
            IdGeneralDocumentType = f.IdGeneralDocumentType,
            IdGeneralDocumentSubtype = f.IdGeneralDocumentSubtype,
            Scope = f.Scope,
            IdOffice = f.IdOffice,
            WithinDays = f.WithinDays,
            IncludeInactive = false
        };

        private static string OfficeOf(GeneralDocumentListDto d) => d.Scope == "OFFICE" ? d.OfficeName ?? "" : "Organisation";
        private static string MaintenanceText(string m) => m switch { "FIXED" => "Fixed", "VERSION" => "Version-controlled", "PERIODIC" => "Periodic", _ => m };
        private static string FrequencyText(string? f) => f switch
        {
            "MONTHLY" => "Monthly", "QUARTERLY" => "Quarterly", "HALFYEARLY" => "Half-yearly", "YEARLY" => "Yearly", "OTHER" => "Other", _ => ""
        };

        private static GeneralDocumentReportColumnDto Col(string key, string label, string type = "text") => new() { Key = key, Label = label, Type = type };

        #region Expiry

        // Current documents with a Valid To date up to ToDate (default today + 90 days); FromDate limits how far back expired ones go
        private async Task<GeneralDocumentReportDto> ExpiryReport(GeneralDocumentReportFilterDto f, GeneralDocumentUserContext user)
        {
            var to = (f.ToDate ?? DateTime.Today.AddDays(90)).Date;
            var docs = (await QueryDocuments(DocumentFilter(f), user))
                .Where(d => d.ValidTo.HasValue && d.ValidTo.Value.Date <= to && (!f.FromDate.HasValue || d.ValidTo.Value.Date >= f.FromDate.Value.Date))
                .OrderBy(d => d.ValidTo).ThenBy(d => d.Title)
                .ToList();

            var today = DateTime.Today;
            var report = new GeneralDocumentReportDto
            {
                Columns = new()
                {
                    Col("code", "Code"), Col("title", "Document"), Col("type", "Type"), Col("subtype", "Subtype"), Col("office", "Office"),
                    Col("current", "Current"), Col("reference", "Reference No."), Col("validFrom", "Valid From", "date"), Col("validTo", "Valid To", "date"),
                    Col("days", "Days Left", "number"), Col("status", "Status"), Col("lastUpload", "Last Upload", "date")
                },
                Rows = docs.Select(d => new Dictionary<string, object?>
                {
                    ["id"] = d.IdGeneralDocument,
                    ["code"] = d.DocumentCode,
                    ["title"] = d.Title,
                    ["type"] = d.TypeName,
                    ["subtype"] = d.SubtypeName,
                    ["office"] = OfficeOf(d),
                    ["current"] = d.PeriodLabel ?? $"Version {d.VersionNumber}",
                    ["reference"] = d.ReferenceNumber,
                    ["validFrom"] = d.ValidFrom,
                    ["validTo"] = d.ValidTo,
                    ["days"] = (d.ValidTo!.Value.Date - today).Days,
                    ["status"] = d.ValidTo.Value.Date < today ? "Expired" : (d.ValidTo.Value.Date - today).Days <= d.ExpiryAlertDays ? "Expiring" : "Due later",
                    ["lastUpload"] = d.LastUploadedDateTime,
                }).ToList()
            };
            report.Summary.Add(new("Expired", report.Rows.Count(r => (string?)r["status"] == "Expired").ToString()));
            report.Summary.Add(new("Within alert period", report.Rows.Count(r => (string?)r["status"] == "Expiring").ToString()));
            report.Summary.Add(new("Due later", report.Rows.Count(r => (string?)r["status"] == "Due later").ToString()));
            return report;
        }

        #endregion

        #region Missing

        private static int? FrequencyMonths(string? f) => f switch { "MONTHLY" => 1, "QUARTERLY" => 3, "HALFYEARLY" => 6, "YEARLY" => 12, _ => null };

        private static string PeriodName(DateTime from, DateTime to, int months) => months switch
        {
            1 => from.ToString("MMM yyyy"),
            12 when from.Month == 4 && from.Day == 1 => $"FY {from.Year}-{(from.Year + 1) % 100:00}",
            12 when from.Month == 1 && from.Day == 1 => from.Year.ToString(),
            _ => $"{from:MMM yyyy} - {to:MMM yyyy}"
        };

        /// <summary>
        /// Documents never uploaded, plus missing periods of periodic documents (monthly / quarterly / half-yearly / yearly).
        /// Expected periods start at the document's earliest uploaded period and repeat by the frequency, up to the last
        /// period that has ended on or before ToDate (default today). FromDate hides older gaps.
        /// </summary>
        private async Task<GeneralDocumentReportDto> MissingReport(GeneralDocumentReportFilterDto f, GeneralDocumentUserContext user)
        {
            var asOf = (f.ToDate ?? DateTime.Today).Date;
            var docs = await QueryDocuments(DocumentFilter(f), user);

            var periodicIds = docs.Where(d => d.MaintenanceType == "PERIODIC" && FrequencyMonths(d.PeriodFrequency) != null).Select(d => d.IdGeneralDocument).ToList();
            var periods = periodicIds.Count == 0
                ? new List<(int Doc, DateTime From, DateTime To)>()
                : (await _db.GeneralDocumentRecords.AsNoTracking()
                    .Where(r => periodicIds.Contains(r.IdGeneralDocument) && r.RecordStatus == Active && r.PeriodFrom != null && r.PeriodTo != null)
                    .Select(r => new { r.IdGeneralDocument, r.PeriodFrom, r.PeriodTo })
                    .ToListAsync())
                    .Select(r => (Doc: r.IdGeneralDocument, From: r.PeriodFrom!.Value.Date, To: r.PeriodTo!.Value.Date)).ToList();

            var rows = new List<Dictionary<string, object?>>();
            Dictionary<string, object?> Row(GeneralDocumentListDto d, string missing, DateTime? from, DateTime? to, string reason) => new()
            {
                ["id"] = d.IdGeneralDocument,
                ["code"] = d.DocumentCode,
                ["title"] = d.Title,
                ["type"] = d.TypeName,
                ["subtype"] = d.SubtypeName,
                ["office"] = OfficeOf(d),
                ["maintenance"] = MaintenanceText(d.MaintenanceType),
                ["frequency"] = FrequencyText(d.PeriodFrequency),
                ["missing"] = missing,
                ["from"] = from,
                ["to"] = to,
                ["reason"] = reason,
                ["lastUpload"] = d.LastUploadedDateTime,
            };

            foreach (var d in docs.OrderBy(d => OfficeOf(d)).ThenBy(d => d.Title))
            {
                if (d.ValidityStatus == "NO_RECORD")
                {
                    rows.Add(Row(d, "Not uploaded", null, null, "No file has been uploaded for this document"));
                    continue;
                }
                var months = FrequencyMonths(d.PeriodFrequency);
                if (d.MaintenanceType != "PERIODIC" || months == null) continue;

                var mine = periods.Where(p => p.Doc == d.IdGeneralDocument).ToList();
                if (mine.Count == 0)
                {
                    rows.Add(Row(d, "No period uploaded", null, null, "Only superseded / incorrect records exist"));
                    continue;
                }
                var start = mine.Min(p => p.From);
                for (var i = 0; i < 240; i++)
                {
                    var pFrom = start.AddMonths(i * months.Value);
                    var pTo = pFrom.AddMonths(months.Value).AddDays(-1);
                    if (pTo > asOf) break;
                    if (f.FromDate.HasValue && pTo < f.FromDate.Value.Date) continue;
                    if (mine.Any(p => p.From <= pTo && p.To >= pFrom)) continue;
                    rows.Add(Row(d, PeriodName(pFrom, pTo, months.Value), pFrom, pTo, "Period not uploaded"));
                }
            }

            var report = new GeneralDocumentReportDto
            {
                Columns = new()
                {
                    Col("code", "Code"), Col("title", "Document"), Col("type", "Type"), Col("subtype", "Subtype"), Col("office", "Office"),
                    Col("maintenance", "Maintenance"), Col("frequency", "Frequency"), Col("missing", "Missing"),
                    Col("from", "Period From", "date"), Col("to", "Period To", "date"), Col("reason", "Reason"), Col("lastUpload", "Last Upload", "date")
                },
                Rows = rows
            };
            report.Summary.Add(new("Documents not uploaded", rows.Count(r => r["from"] == null).ToString()));
            report.Summary.Add(new("Missing periods", rows.Count(r => r["from"] != null).ToString()));
            report.Summary.Add(new("Documents affected", rows.Select(r => r["id"]).Distinct().Count().ToString()));
            return report;
        }

        #endregion

        #region Compliance

        private async Task<GeneralDocumentReportDto> ComplianceReport(GeneralDocumentReportFilterDto f, GeneralDocumentUserContext user)
        {
            var byType = string.Equals(f.GroupBy, "TYPE", StringComparison.OrdinalIgnoreCase);
            var docs = await QueryDocuments(DocumentFilter(f), user);

            // missing periods count per document (same rule as the Missing report)
            var missing = (await MissingReport(new GeneralDocumentReportFilterDto
            {
                ReportCode = "MISSING",
                IdGeneralDocumentType = f.IdGeneralDocumentType,
                IdGeneralDocumentSubtype = f.IdGeneralDocumentSubtype,
                Scope = f.Scope,
                IdOffice = f.IdOffice,
                ToDate = f.ToDate
            }, user)).Rows.Where(r => r["from"] != null).GroupBy(r => (int)r["id"]!).ToDictionary(g => g.Key, g => g.Count());

            Dictionary<string, object?> Line(string group, List<GeneralDocumentListDto> list)
            {
                int C(string s) => list.Count(d => d.ValidityStatus == s);
                var ok = C("VALID") + C("NO_EXPIRY");
                var gaps = list.Sum(d => missing.TryGetValue(d.IdGeneralDocument, out var n) ? n : 0);
                var docsWithGaps = list.Count(d => missing.ContainsKey(d.IdGeneralDocument) && (d.ValidityStatus is "VALID" or "NO_EXPIRY"));
                var compliant = ok - docsWithGaps;
                return new Dictionary<string, object?>
                {
                    ["group"] = group,
                    ["total"] = list.Count,
                    ["valid"] = C("VALID"),
                    ["noExpiry"] = C("NO_EXPIRY"),
                    ["expiring"] = C("EXPIRING"),
                    ["expired"] = C("EXPIRED"),
                    ["notUploaded"] = C("NO_RECORD"),
                    ["missingPeriods"] = gaps,
                    ["compliant"] = compliant,
                    ["percent"] = list.Count == 0 ? 0m : Math.Round(compliant * 100m / list.Count, 1),
                };
            }

            var rows = docs
                .GroupBy(d => byType ? d.TypeName ?? "" : OfficeOf(d))
                .OrderBy(g => byType ? g.Key : (g.Key == "Organisation" ? "" : g.Key))
                .Select(g => Line(g.Key, g.ToList()))
                .ToList();
            var total = Line("Total", docs);

            var report = new GeneralDocumentReportDto
            {
                Columns = new()
                {
                    Col("group", byType ? "Document Type" : "Office"), Col("total", "Documents", "number"), Col("valid", "Valid", "number"),
                    Col("noExpiry", "No Expiry", "number"), Col("expiring", "Expiring", "number"), Col("expired", "Expired", "number"),
                    Col("notUploaded", "Not Uploaded", "number"), Col("missingPeriods", "Missing Periods", "number"),
                    Col("compliant", "Compliant", "number"), Col("percent", "Compliance %", "percent")
                },
                Rows = rows
            };
            if (rows.Count > 1) report.Rows.Add(total);
            report.Summary.Add(new("Documents", total["total"]!.ToString()!));
            report.Summary.Add(new("Compliant", total["compliant"]!.ToString()!));
            report.Summary.Add(new("Compliance", $"{total["percent"]:0.#}%"));
            report.Summary.Add(new("Rule", "Compliant = valid or no expiry, and no missing periods"));
            return report;
        }

        #endregion

        #region Activity

        private async Task<GeneralDocumentReportDto> ActivityReport(GeneralDocumentReportFilterDto f, GeneralDocumentUserContext user)
        {
            var fromDate = (f.FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var toExcl = (f.ToDate ?? DateTime.Today).Date.AddDays(1);
            if ((toExcl - fromDate).TotalDays > 400) throw new InvalidOperationException("Select a date range of up to one year.");

            var offices = await AccessibleOffices(user);
            var officeIds = offices?.ToList();
            var action = string.IsNullOrWhiteSpace(f.Action) ? null : f.Action.Trim().ToUpperInvariant();
            var scope = string.IsNullOrWhiteSpace(f.Scope) ? null : f.Scope.Trim().ToUpperInvariant();
            var viewActions = new[] { "VIEW", "DOWNLOAD", "ZIP_DOWNLOAD" };

            var q =
                from a in _db.GeneralDocumentAudit.AsNoTracking()
                join d in _db.GeneralDocuments.AsNoTracking() on a.IdGeneralDocument equals (int?)d.IdGeneralDocument
                where a.ActionDateTime >= fromDate && a.ActionDateTime < toExcl
                select new { a, d };
            if (officeIds != null) q = q.Where(x => x.d.Scope == "ORGANIZATION" || (x.d.IdOffice != null && officeIds.Contains(x.d.IdOffice.Value)));
            if (action != null) q = q.Where(x => x.a.Action == action);
            else if (!f.IncludeViews) q = q.Where(x => !viewActions.Contains(x.a.Action));
            if (f.IdGeneralDocumentType is > 0) q = q.Where(x => x.d.IdGeneralDocumentType == f.IdGeneralDocumentType);
            if (f.IdGeneralDocumentSubtype is > 0) q = q.Where(x => x.d.IdGeneralDocumentSubtype == f.IdGeneralDocumentSubtype);
            if (scope != null) q = q.Where(x => x.d.Scope == scope);
            if (f.IdOffice is > 0) q = q.Where(x => x.d.IdOffice == f.IdOffice);

            var data = await q
                .OrderByDescending(x => x.a.ActionDateTime).ThenByDescending(x => x.a.IdGeneralDocumentAudit)
                .Take(MaxReportRows + 1)
                .Select(x => new
                {
                    x.a.ActionDateTime,
                    x.a.Action,
                    x.a.Details,
                    x.a.ActionBy,
                    x.a.IPAddress,
                    x.d.IdGeneralDocument,
                    x.d.DocumentCode,
                    x.d.Title,
                    x.d.Scope,
                    OfficeName = _db.Offices.Where(o => o.IdOffice == x.d.IdOffice).Select(o => o.OfficeName).FirstOrDefault(),
                    Version = _db.GeneralDocumentRecords.Where(r => r.IdGeneralDocumentRecord == x.a.IdGeneralDocumentRecord).Select(r => (int?)r.VersionNumber).FirstOrDefault()
                })
                .ToListAsync();

            var names = await EmployeeNames(data.Select(x => x.ActionBy));
            var report = new GeneralDocumentReportDto
            {
                Columns = new()
                {
                    Col("when", "Date / Time", "datetime"), Col("action", "Action"), Col("code", "Code"), Col("title", "Document"),
                    Col("office", "Office"), Col("version", "Version", "number"), Col("by", "Done By"), Col("details", "Details"), Col("ip", "IP Address")
                },
                Rows = data.Select(x => new Dictionary<string, object?>
                {
                    ["id"] = x.IdGeneralDocument,
                    ["when"] = x.ActionDateTime,
                    ["action"] = ActionText.TryGetValue(x.Action, out var t) ? t : x.Action,
                    ["code"] = x.DocumentCode,
                    ["title"] = x.Title,
                    ["office"] = x.Scope == "OFFICE" ? x.OfficeName : "Organisation",
                    ["version"] = x.Version,
                    ["by"] = Name(names, x.ActionBy),
                    ["details"] = DescribeDetails(x.Details),
                    ["ip"] = x.IPAddress,
                }).ToList()
            };
            foreach (var g in report.Rows.GroupBy(r => (string)r["action"]!).OrderByDescending(g => g.Count()))
                report.Summary.Add(new(g.Key, g.Count().ToString()));
            return report;
        }

        // {"Title":{"From":"a","To":"b"},"Files":[{"OriginalFileName":"x.pdf"}]} -> "Title: a → b; Files: x.pdf"
        private static string? DescribeDetails(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return json;
                static string Val(JsonElement e) => e.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => "—",
                    JsonValueKind.String => DateTime.TryParse(e.GetString(), out var dt) && e.GetString()!.Contains('T')
                        ? (dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("dd-MMM-yyyy") : dt.ToString("dd-MMM-yyyy HH:mm"))
                        : e.GetString() ?? "",
                    JsonValueKind.Array => string.Join(", ", e.EnumerateArray().Select(x =>
                        x.ValueKind == JsonValueKind.Object && x.TryGetProperty("OriginalFileName", out var n) ? n.GetString() : Val(x))),
                    JsonValueKind.Object => e.TryGetProperty("From", out var a) && e.TryGetProperty("To", out var b) ? $"{Val(a)} → {Val(b)}" : e.ToString(),
                    _ => e.ToString()
                };
                var parts = new List<string>();
                foreach (var p in doc.RootElement.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Null || (p.Value.ValueKind == JsonValueKind.Array && p.Value.GetArrayLength() == 0)) continue;
                    if (p.Name == "Changes" && p.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var c in p.Value.EnumerateObject()) parts.Add($"{c.Name}: {Val(c.Value)}");
                        continue;
                    }
                    parts.Add($"{p.Name}: {Val(p.Value)}");
                }
                return string.Join("; ", parts);
            }
            catch (JsonException)
            {
                return json;
            }
        }

        #endregion

        #region Excel

        private async Task<string> CriteriaText(GeneralDocumentReportFilterDto f)
        {
            var parts = new List<string>();
            var code = f.ReportCode?.Trim().ToUpperInvariant();
            if (code == "EXPIRY") parts.Add($"Valid To {(f.FromDate.HasValue ? $"from {f.FromDate:dd-MMM-yyyy} " : "")}up to {(f.ToDate ?? DateTime.Today.AddDays(90)):dd-MMM-yyyy}");
            if (code == "MISSING" || code == "COMPLIANCE") parts.Add($"As on {(f.ToDate ?? DateTime.Today):dd-MMM-yyyy}");
            if (code == "MISSING" && f.FromDate.HasValue) parts.Add($"periods ending from {f.FromDate:dd-MMM-yyyy}");
            if (code == "COMPLIANCE") parts.Add(f.WithinDays.HasValue ? $"Expiring = within {f.WithinDays} days" : "Expiring = within each document's alert days");
            if (code == "ACTIVITY") parts.Add($"{(f.FromDate ?? DateTime.Today.AddDays(-30)):dd-MMM-yyyy} to {(f.ToDate ?? DateTime.Today):dd-MMM-yyyy}");
            if (f.IdGeneralDocumentType is > 0)
                parts.Add("Type: " + await _db.GeneralDocumentTypes.Where(t => t.IdGeneralDocumentType == f.IdGeneralDocumentType).Select(t => t.TypeName).FirstOrDefaultAsync());
            if (f.IdGeneralDocumentSubtype is > 0)
                parts.Add("Subtype: " + await _db.GeneralDocumentSubtypes.Where(s => s.IdGeneralDocumentSubtype == f.IdGeneralDocumentSubtype).Select(s => s.SubtypeName).FirstOrDefaultAsync());
            if (!string.IsNullOrWhiteSpace(f.Scope)) parts.Add(f.Scope.Trim().ToUpperInvariant() == "OFFICE" ? "Office documents" : "Organisation documents");
            if (f.IdOffice is > 0)
                parts.Add("Office: " + await _db.Offices.Where(o => o.IdOffice == f.IdOffice).Select(o => o.OfficeName).FirstOrDefaultAsync());
            if (code == "ACTIVITY" && !string.IsNullOrWhiteSpace(f.Action)) parts.Add("Action: " + (ActionText.TryGetValue(f.Action.Trim().ToUpperInvariant(), out var a) ? a : f.Action));
            return string.Join(" · ", parts);
        }

        public async Task<FileDownloadDto> ExportReport(GeneralDocumentReportFilterDto filter, GeneralDocumentUserContext user)
        {
            var report = await GetReport(filter, user);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(report.Title.Length > 31 ? report.Title[..31] : report.Title);
            var cols = report.Columns;
            var lastCol = Math.Max(cols.Count, 1);
            var header = Color.FromArgb(15, 60, 84);   // #0f3c54

            ws.Cells[1, 1].Value = report.Title;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[2, 1].Value = report.Criteria;
            ws.Cells[3, 1].Value = $"Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}";
            ws.Cells[2, 1, 3, 1].Style.Font.Color.SetColor(Color.DimGray);

            var row = 5;
            if (report.Summary.Count > 0)
            {
                foreach (var s in report.Summary)
                {
                    ws.Cells[row, 1].Value = s.Key;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 2].Value = int.TryParse(s.Value, out var n) ? n : s.Value;
                    row++;
                }
                row++;
            }

            var headerRow = row;
            for (var c = 0; c < cols.Count; c++) ws.Cells[headerRow, c + 1].Value = cols[c].Label;
            using (var h = ws.Cells[headerRow, 1, headerRow, lastCol])
            {
                h.Style.Font.Bold = true;
                h.Style.Font.Color.SetColor(Color.White);
                h.Style.Fill.PatternType = ExcelFillStyle.Solid;
                h.Style.Fill.BackgroundColor.SetColor(header);
            }

            foreach (var r in report.Rows)
            {
                row++;
                for (var c = 0; c < cols.Count; c++)
                {
                    r.TryGetValue(cols[c].Key, out var v);
                    var cell = ws.Cells[row, c + 1];
                    switch (cols[c].Type)
                    {
                        case "date":
                            cell.Value = v as DateTime?;
                            cell.Style.Numberformat.Format = "dd-mmm-yyyy";
                            break;
                        case "datetime":
                            cell.Value = v as DateTime?;
                            cell.Style.Numberformat.Format = "dd-mmm-yyyy hh:mm";
                            break;
                        case "percent":
                            cell.Value = v == null ? null : Convert.ToDouble(v) / 100d;
                            cell.Style.Numberformat.Format = "0.0%";
                            break;
                        default:
                            cell.Value = v;
                            break;
                    }
                }
                if (report.ReportCode == "COMPLIANCE" && Equals(r.GetValueOrDefault("group"), "Total"))
                    ws.Cells[row, 1, row, lastCol].Style.Font.Bold = true;
            }

            if (report.Rows.Count > 0)
            {
                ws.Cells[headerRow, 1, row, lastCol].AutoFilter = true;
                ws.Cells[headerRow, 1, row, lastCol].AutoFitColumns(8, 60);
            }
            if (report.Truncated)
                ws.Cells[row + 2, 1].Value = $"Only the first {MaxReportRows} rows are included. Narrow the filters to see the rest.";
            ws.View.FreezePanes(headerRow + 1, 1);

            AddAudit(null, null, null, "REPORT_EXPORT", new { report.ReportCode, report.Criteria, Rows = report.Rows.Count }, user);
            await _db.SaveChangesAsync();

            return new FileDownloadDto
            {
                Content = await package.GetAsByteArrayAsync(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"{report.Title.Replace(" / ", "_").Replace(" & ", "_").Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
        }

        #endregion
    }
}
