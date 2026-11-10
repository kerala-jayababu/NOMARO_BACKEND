using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Public documents: subtypes marked "Visible to All Employees" appear in Self Portal -> Documents.
    /// When the subtype also has "Acknowledgement Required", employees open the file and click "Viewed and Acknowledged".
    /// Acknowledgement is per version (record): a new upload has to be acknowledged again.
    /// Audience: working employees; an office document goes to that office and its child offices.
    /// </summary>
    public partial class GeneralDocumentService
    {
        private const string TAckReminder = "DOCACKREMINDER";

        private sealed record PublicDoc(GeneralDocuments Doc, GeneralDocumentRecords Record, string? TypeName, string? SubtypeName,
            string? OfficeName, bool AckRequired);

        private sealed record AudienceEmployee(int IdEmployee, int? IdOffice);

        #region Office tree

        private async Task<List<(int Id, int? Parent)>> OfficeTree() =>
            (await _db.Offices.AsNoTracking().Select(o => new { o.IdOffice, o.IdParentOffice }).ToListAsync())
                .Select(o => (o.IdOffice, o.IdParentOffice)).ToList();

        private static HashSet<int> Descendants(List<(int Id, int? Parent)> tree, int root)
        {
            var result = new HashSet<int> { root };
            var queue = new Queue<int>(result);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var child in tree.Where(o => o.Parent == id && o.Id != id))
                    if (result.Add(child.Id)) queue.Enqueue(child.Id);
            }
            return result;
        }

        private static HashSet<int> AncestorsOrSelf(List<(int Id, int? Parent)> tree, int office)
        {
            var result = new HashSet<int>();
            int? current = office;
            while (current.HasValue && result.Add(current.Value))
                current = tree.FirstOrDefault(o => o.Id == current.Value).Parent;
            return result;
        }

        #endregion

        /// <summary>Active documents of a "Visible to All Employees" subtype that have a current version with files.</summary>
        private async Task<List<PublicDoc>> PublicDocuments(int? idType = null, int? idDocument = null)
        {
            var q =
                from d in _db.GeneralDocuments.AsNoTracking()
                join s in _db.GeneralDocumentSubtypes.AsNoTracking() on d.IdGeneralDocumentSubtype equals (int?)s.IdGeneralDocumentSubtype
                join r in _db.GeneralDocumentRecords.AsNoTracking() on d.IdGeneralDocument equals r.IdGeneralDocument
                where d.IsActive && s.VisibleToAllEmployees && r.IsCurrent && r.RecordStatus == Active
                      && _db.GeneralDocumentFiles.Any(f => f.IdGeneralDocumentRecord == r.IdGeneralDocumentRecord)
                select new
                {
                    d,
                    r,
                    s.SubtypeName,
                    s.AcknowledgementRequired,
                    TypeName = _db.GeneralDocumentTypes.Where(t => t.IdGeneralDocumentType == d.IdGeneralDocumentType).Select(t => t.TypeName).FirstOrDefault(),
                    OfficeName = _db.Offices.Where(o => o.IdOffice == d.IdOffice).Select(o => o.OfficeName).FirstOrDefault()
                };
            if (idType is > 0) q = q.Where(x => x.d.IdGeneralDocumentType == idType);
            if (idDocument is > 0) q = q.Where(x => x.d.IdGeneralDocument == idDocument);

            return (await q.ToListAsync())
                .Select(x => new PublicDoc(x.d, x.r, x.TypeName, x.SubtypeName, x.d.Scope == "OFFICE" ? x.OfficeName : null, x.AcknowledgementRequired))
                .ToList();
        }

        private async Task<List<AudienceEmployee>> WorkingEmployees() =>
            (await _db.Employees.AsNoTracking()
                .Where(e => e.IdEmployee != null && e.CurrentStatus == "Working")
                .Select(e => new { Id = e.IdEmployee!.Value, e.IdOffice })
                .ToListAsync())
            .Select(e => new AudienceEmployee(e.Id, e.IdOffice)).ToList();

        private static List<AudienceEmployee> AudienceOf(PublicDoc p, List<AudienceEmployee> employees, List<(int Id, int? Parent)> tree)
        {
            if (p.Doc.Scope != "OFFICE" || !p.Doc.IdOffice.HasValue) return employees;
            var offices = Descendants(tree, p.Doc.IdOffice.Value);
            return employees.Where(e => e.IdOffice.HasValue && offices.Contains(e.IdOffice.Value)).ToList();
        }

        #region Self Portal

        private async Task<List<PublicDoc>> VisibleTo(int idEmployee)
        {
            var myOffice = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == idEmployee).Select(e => e.IdOffice).FirstOrDefaultAsync();
            var mine = myOffice.HasValue ? AncestorsOrSelf(await OfficeTree(), myOffice.Value) : new HashSet<int>();
            return (await PublicDocuments())
                .Where(p => p.Doc.Scope == "ORGANIZATION" || (p.Doc.IdOffice.HasValue && mine.Contains(p.Doc.IdOffice.Value)))
                .ToList();
        }

        public async Task<List<MyPublicDocumentDto>> GetMyDocuments(int idEmployee)
        {
            var docs = await VisibleTo(idEmployee);
            var recordIds = docs.Select(p => p.Record.IdGeneralDocumentRecord).ToList();
            var files = await _db.GeneralDocumentFiles.AsNoTracking().Where(f => recordIds.Contains(f.IdGeneralDocumentRecord)).OrderBy(f => f.IdGeneralDocumentFile).ToListAsync();
            var acks = await _db.GeneralDocumentAcknowledgements.AsNoTracking()
                .Where(a => a.IdEmployee == idEmployee && recordIds.Contains(a.IdGeneralDocumentRecord)).ToListAsync();

            return docs
                .Select(p =>
                {
                    var ack = acks.FirstOrDefault(a => a.IdGeneralDocumentRecord == p.Record.IdGeneralDocumentRecord);
                    return new MyPublicDocumentDto
                    {
                        IdGeneralDocument = p.Doc.IdGeneralDocument,
                        IdGeneralDocumentRecord = p.Record.IdGeneralDocumentRecord,
                        DocumentCode = p.Doc.DocumentCode,
                        Title = p.Doc.Title,
                        Description = p.Doc.Description,
                        TypeName = p.TypeName,
                        SubtypeName = p.SubtypeName,
                        OfficeName = p.OfficeName,
                        VersionNumber = p.Record.VersionNumber,
                        PeriodLabel = p.Record.PeriodLabel,
                        ValidFrom = p.Record.ValidFrom,
                        ValidTo = p.Record.ValidTo,
                        PublishedDateTime = p.Record.UploadedDateTime,
                        AcknowledgementRequired = p.AckRequired,
                        FirstViewedDateTime = ack?.FirstViewedDateTime,
                        AcknowledgedDateTime = ack?.AcknowledgedDateTime,
                        Files = files.Where(f => f.IdGeneralDocumentRecord == p.Record.IdGeneralDocumentRecord).Select(f => new GeneralDocumentFileDto
                        {
                            IdGeneralDocumentFile = f.IdGeneralDocumentFile,
                            OriginalFileName = f.OriginalFileName,
                            MimeType = f.MimeType,
                            FileSize = f.FileSize,
                            UploadedDateTime = f.UploadedDateTime
                        }).ToList()
                    };
                })
                // pending acknowledgements first, then newest
                .OrderBy(d => d.AcknowledgementRequired && d.AcknowledgedDateTime == null ? 0 : 1)
                .ThenByDescending(d => d.PublishedDateTime)
                .ToList();
        }

        private static string? Trim(string? value, int max) => string.IsNullOrEmpty(value) ? value : value.Length > max ? value[..max] : value;

        public async Task<FileDownloadDto> GetMyDocumentFile(int idGeneralDocumentFile, int idEmployee, string? ip, string? userAgent)
        {
            var file = await _db.GeneralDocumentFiles.AsNoTracking().FirstOrDefaultAsync(f => f.IdGeneralDocumentFile == idGeneralDocumentFile)
                ?? throw new KeyNotFoundException("Document not found.");
            var doc = (await VisibleTo(idEmployee)).FirstOrDefault(p => p.Record.IdGeneralDocumentRecord == file.IdGeneralDocumentRecord)
                ?? throw new KeyNotFoundException("Document not found.");

            var path = FullPath(file.StoredFileName);
            if (path == null || !File.Exists(path)) throw new KeyNotFoundException("The file was not found on the server.");

            var now = DateTime.Now;
            var ack = await _db.GeneralDocumentAcknowledgements
                .FirstOrDefaultAsync(a => a.IdGeneralDocumentRecord == file.IdGeneralDocumentRecord && a.IdEmployee == idEmployee);
            if (ack == null)
            {
                ack = new GeneralDocumentAcknowledgements
                {
                    IdGeneralDocument = doc.Doc.IdGeneralDocument,
                    IdGeneralDocumentRecord = file.IdGeneralDocumentRecord,
                    IdEmployee = idEmployee,
                    FirstViewedDateTime = now
                };
                _db.GeneralDocumentAcknowledgements.Add(ack);
            }
            ack.FirstViewedDateTime ??= now;
            ack.LastViewedDateTime = now;
            ack.ViewCount++;
            if (ack.AcknowledgedDateTime == null)
            {
                ack.IPAddress = Trim(ip, 50);
                ack.UserAgent = Trim(userAgent, 300);
            }
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex)
            {
                // the same employee opened two files at once: the other request created the row
                _logger.LogWarning(ex, "Document view of employee {Employee} not recorded", idEmployee);
            }

            return new FileDownloadDto
            {
                Content = await File.ReadAllBytesAsync(path),
                ContentType = file.MimeType ?? MimeOf(Path.GetExtension(file.OriginalFileName)),
                FileName = file.OriginalFileName
            };
        }

        public async Task<DateTime> AcknowledgeMyDocument(int idGeneralDocumentRecord, int idEmployee, string? ip, string? userAgent)
        {
            var doc = (await VisibleTo(idEmployee)).FirstOrDefault(p => p.Record.IdGeneralDocumentRecord == idGeneralDocumentRecord)
                ?? throw new InvalidOperationException("This document is no longer available. Refresh the page; a newer version may have been published.");
            if (!doc.AckRequired) throw new InvalidOperationException("This document does not need an acknowledgement.");

            var ack = await _db.GeneralDocumentAcknowledgements
                .FirstOrDefaultAsync(a => a.IdGeneralDocumentRecord == idGeneralDocumentRecord && a.IdEmployee == idEmployee);
            if (ack?.FirstViewedDateTime == null) throw new InvalidOperationException("Open and read the document before acknowledging it.");
            if (ack.AcknowledgedDateTime.HasValue) return ack.AcknowledgedDateTime.Value;

            ack.AcknowledgedDateTime = DateTime.Now;
            ack.IPAddress = Trim(ip, 50);
            ack.UserAgent = Trim(userAgent, 300);
            await _db.SaveChangesAsync();
            return ack.AcknowledgedDateTime.Value;
        }

        #endregion

        #region Dashboard

        /// <summary>Public documents the HR user can see (same office access as the register).</summary>
        private async Task<List<PublicDoc>> DashboardDocuments(GeneralDocumentUserContext user, int? idType = null, int? idDocument = null)
        {
            var offices = await AccessibleOffices(user);
            return (await PublicDocuments(idType, idDocument)).Where(p => CanSee(p.Doc, offices)).ToList();
        }

        private static decimal Percent(int part, int whole) => whole == 0 ? 0 : Math.Round(part * 100m / whole, 1);

        public async Task<AckDashboardDto> GetAckDashboard(int? idGeneralDocumentType, GeneralDocumentUserContext user)
        {
            var docs = await DashboardDocuments(user, idGeneralDocumentType);
            var employees = await WorkingEmployees();
            var tree = await OfficeTree();
            var recordIds = docs.Select(p => p.Record.IdGeneralDocumentRecord).ToList();
            var acks = await _db.GeneralDocumentAcknowledgements.AsNoTracking()
                .Where(a => recordIds.Contains(a.IdGeneralDocumentRecord))
                .Select(a => new { a.IdGeneralDocumentRecord, a.IdEmployee, a.FirstViewedDateTime, a.AcknowledgedDateTime })
                .ToListAsync();

            var result = new AckDashboardDto { CanSendReminder = user.CanSendAckReminder };
            foreach (var p in docs.OrderByDescending(p => p.AckRequired).ThenByDescending(p => p.Record.UploadedDateTime))
            {
                var audience = AudienceOf(p, employees, tree).Select(e => e.IdEmployee).ToHashSet();
                var mine = acks.Where(a => a.IdGeneralDocumentRecord == p.Record.IdGeneralDocumentRecord && audience.Contains(a.IdEmployee)).ToList();
                var acked = p.AckRequired ? mine.Count(a => a.AcknowledgedDateTime.HasValue) : 0;
                var viewed = mine.Count(a => a.FirstViewedDateTime.HasValue);
                var viewedOnly = p.AckRequired ? mine.Count(a => a.FirstViewedDateTime.HasValue && !a.AcknowledgedDateTime.HasValue) : viewed;
                var row = new AckDocumentSummaryDto
                {
                    IdGeneralDocument = p.Doc.IdGeneralDocument,
                    IdGeneralDocumentRecord = p.Record.IdGeneralDocumentRecord,
                    DocumentCode = p.Doc.DocumentCode,
                    Title = p.Doc.Title,
                    TypeName = p.TypeName,
                    SubtypeName = p.SubtypeName,
                    OfficeName = p.OfficeName,
                    VersionNumber = p.Record.VersionNumber,
                    PeriodLabel = p.Record.PeriodLabel,
                    PublishedDateTime = p.Record.UploadedDateTime,
                    AcknowledgementRequired = p.AckRequired,
                    Audience = audience.Count,
                    Acknowledged = acked,
                    ViewedOnly = viewedOnly,
                    NotViewed = audience.Count - acked - viewedOnly,
                    AcknowledgedPercent = Percent(acked, audience.Count),
                    ViewedPercent = Percent(viewed, audience.Count)
                };
                result.Documents.Add(row);
                if (p.AckRequired)
                {
                    result.TotalExpected += row.Audience;
                    result.TotalAcknowledged += row.Acknowledged;
                    result.TotalViewedOnly += row.ViewedOnly;
                    result.TotalNotViewed += row.NotViewed;
                }
            }
            result.AcknowledgedPercent = Percent(result.TotalAcknowledged, result.TotalExpected);
            return result;
        }

        public async Task<List<AckEmployeeDto>> GetAckEmployees(int idGeneralDocument, GeneralDocumentUserContext user)
        {
            var p = (await DashboardDocuments(user, null, idGeneralDocument)).FirstOrDefault()
                ?? throw new KeyNotFoundException("Document not found, or it is not visible to employees.");
            var audience = AudienceOf(p, await WorkingEmployees(), await OfficeTree()).Select(e => e.IdEmployee).ToList();

            var acks = await _db.GeneralDocumentAcknowledgements.AsNoTracking()
                .Where(a => a.IdGeneralDocumentRecord == p.Record.IdGeneralDocumentRecord)
                .ToDictionaryAsync(a => a.IdEmployee);

            var people = await (
                from e in _db.Employees.AsNoTracking()
                where e.IdEmployee != null && audience.Contains(e.IdEmployee.Value)
                select new
                {
                    Id = e.IdEmployee!.Value,
                    e.EmployeeCode,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName,
                    e.EmailID,
                    e.HomeEmail,
                    Dept = _db.Departments.Where(d => (int?)d.IdDepartment == e.IdDepartment).Select(d => d.DepartmentName).FirstOrDefault(),
                    Desig = _db.Designations.Where(d => (int?)d.IdDesignation == e.IdDesignation).Select(d => d.DesignationName).FirstOrDefault(),
                    Office = _db.Offices.Where(o => o.IdOffice == e.IdOffice).Select(o => o.OfficeName).FirstOrDefault()
                }).ToListAsync();

            return people.Select(x =>
            {
                acks.TryGetValue(x.Id, out var a);
                var status = p.AckRequired && a?.AcknowledgedDateTime != null ? "ACKNOWLEDGED" : a?.FirstViewedDateTime != null ? "VIEWED" : "NOT_VIEWED";
                return new AckEmployeeDto
                {
                    IdEmployee = x.Id,
                    EmployeeCode = x.EmployeeCode,
                    EmployeeName = string.Join(" ", new[] { x.FirstName, x.MiddleName, x.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                    DepartmentName = x.Dept,
                    DesignationName = x.Desig,
                    OfficeName = x.Office,
                    Email = !string.IsNullOrWhiteSpace(x.EmailID) ? x.EmailID : x.HomeEmail,
                    Status = status,
                    FirstViewedDateTime = a?.FirstViewedDateTime,
                    AcknowledgedDateTime = p.AckRequired ? a?.AcknowledgedDateTime : null
                };
            })
            .OrderBy(x => x.Status == "ACKNOWLEDGED" ? 1 : 0).ThenBy(x => x.EmployeeName)
            .ToList();
        }

        public async Task<FileDownloadDto> ExportAckEmployees(int idGeneralDocument, string? status, GeneralDocumentUserContext user)
        {
            var p = (await DashboardDocuments(user, null, idGeneralDocument)).FirstOrDefault()
                ?? throw new KeyNotFoundException("Document not found, or it is not visible to employees.");
            var all = await GetAckEmployees(idGeneralDocument, user);
            var filter = (status ?? "NOT_ACKNOWLEDGED").Trim().ToUpperInvariant();
            var rows = filter switch
            {
                "ALL" => all,
                "ACKNOWLEDGED" => all.Where(e => e.Status == "ACKNOWLEDGED").ToList(),
                _ => all.Where(e => e.Status != "ACKNOWLEDGED").ToList()
            };
            var label = filter switch { "ALL" => "All employees", "ACKNOWLEDGED" => "Acknowledged", _ => "Not acknowledged" };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(label);
            ws.Cells[1, 1].Value = $"{p.Doc.Title} - {label}";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[2, 1].Value = $"{p.Doc.DocumentCode} · Version {p.Record.VersionNumber}{(string.IsNullOrWhiteSpace(p.Record.PeriodLabel) ? "" : " · " + p.Record.PeriodLabel)} · Published {p.Record.UploadedDateTime:dd-MMM-yyyy}";
            ws.Cells[3, 1].Value = $"{rows.Count} of {all.Count} employees · Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}";
            ws.Cells[2, 1, 3, 1].Style.Font.Color.SetColor(Color.DimGray);

            string[] head = { "Employee Code", "Employee Name", "Department", "Designation", "Office", "Email", "Status", "First Viewed", "Acknowledged On" };
            const int hr = 5;
            for (var c = 0; c < head.Length; c++) ws.Cells[hr, c + 1].Value = head[c];
            using (var h = ws.Cells[hr, 1, hr, head.Length])
            {
                h.Style.Font.Bold = true;
                h.Style.Font.Color.SetColor(Color.White);
                h.Style.Fill.PatternType = ExcelFillStyle.Solid;
                h.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(15, 60, 84));
            }
            var row = hr;
            foreach (var e in rows)
            {
                row++;
                ws.Cells[row, 1].Value = e.EmployeeCode;
                ws.Cells[row, 2].Value = e.EmployeeName;
                ws.Cells[row, 3].Value = e.DepartmentName;
                ws.Cells[row, 4].Value = e.DesignationName;
                ws.Cells[row, 5].Value = e.OfficeName;
                ws.Cells[row, 6].Value = e.Email;
                ws.Cells[row, 7].Value = e.Status switch { "ACKNOWLEDGED" => "Acknowledged", "VIEWED" => "Viewed, not acknowledged", _ => "Not viewed" };
                ws.Cells[row, 8].Value = e.FirstViewedDateTime;
                ws.Cells[row, 9].Value = e.AcknowledgedDateTime;
                ws.Cells[row, 8, row, 9].Style.Numberformat.Format = "dd-mmm-yyyy hh:mm";
            }
            if (rows.Count > 0)
            {
                ws.Cells[hr, 1, row, head.Length].AutoFilter = true;
                ws.Cells[hr, 1, row, head.Length].AutoFitColumns(8, 50);
            }
            ws.View.FreezePanes(hr + 1, 1);

            AddAudit(p.Doc.IdGeneralDocument, p.Record.IdGeneralDocumentRecord, null, "ACK_EXPORT", new { Status = label, Rows = rows.Count }, user);
            await _db.SaveChangesAsync();

            var safe = new string(p.Doc.Title.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
            return new FileDownloadDto
            {
                Content = await package.GetAsByteArrayAsync(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"{(safe.Length > 40 ? safe[..40] : safe)}_{label.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.xlsx"
            };
        }

        #endregion

        #region Reminders

        public async Task<AckReminderResultDto> SendAckReminders(AckReminderRequestDto request, GeneralDocumentUserContext user)
        {
            if (!user.CanSendAckReminder) throw new UnauthorizedAccessException();
            var docs = (await DashboardDocuments(user, null, request?.IdGeneralDocument)).Where(p => p.AckRequired).ToList();
            if (docs.Count == 0)
                throw new InvalidOperationException(request?.IdGeneralDocument is > 0
                    ? "This document does not need an acknowledgement."
                    : "There are no published documents that need an acknowledgement.");

            var employees = await WorkingEmployees();
            var tree = await OfficeTree();
            var recordIds = docs.Select(p => p.Record.IdGeneralDocumentRecord).ToList();
            var acked = (await _db.GeneralDocumentAcknowledgements.AsNoTracking()
                    .Where(a => recordIds.Contains(a.IdGeneralDocumentRecord) && a.AcknowledgedDateTime != null)
                    .Select(a => new { a.IdGeneralDocumentRecord, a.IdEmployee }).ToListAsync())
                .Select(a => (a.IdGeneralDocumentRecord, a.IdEmployee)).ToHashSet();

            // employee -> documents still to acknowledge
            var pending = new Dictionary<int, List<PublicDoc>>();
            foreach (var p in docs)
                foreach (var e in AudienceOf(p, employees, tree))
                    if (!acked.Contains((p.Record.IdGeneralDocumentRecord, e.IdEmployee)))
                    {
                        if (!pending.TryGetValue(e.IdEmployee, out var list)) pending[e.IdEmployee] = list = new List<PublicDoc>();
                        list.Add(p);
                    }
            if (request?.EmployeeIds is { Count: > 0 })
            {
                var only = request.EmployeeIds.ToHashSet();
                pending = pending.Where(kv => only.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            if (pending.Count == 0) throw new InvalidOperationException("Everyone has acknowledged; there is no one to remind.");
            if (pending.Count > 3000) throw new InvalidOperationException("More than 3000 employees would be emailed at once. Send reminders per document instead.");

            var ids = pending.Keys.ToList();
            var people = await _db.Employees.AsNoTracking()
                .Where(e => e.IdEmployee != null && ids.Contains(e.IdEmployee.Value))
                .Select(e => new { Id = e.IdEmployee!.Value, e.EmployeeCode, e.FirstName, e.LastName, e.EmailID, e.HomeEmail })
                .ToListAsync();

            var company = await _db.SystemParameters.AsNoTracking().Where(x => x.ParameterName == "CompanyName").Select(x => x.ParameterValue).FirstOrDefaultAsync() ?? "NOMARO HRMS";
            var link = await _db.SystemParameters.AsNoTracking().Where(x => x.ParameterName == "AGREEMENTPORTALURL").Select(x => x.ParameterValue).FirstOrDefaultAsync() ?? "";
            var config = await _db.NotificationsConfig.AsNoTracking().FirstOrDefaultAsync(n => n.EntityCode == TAckReminder);
            if (config == null) _logger.LogWarning("Email template {Code} not found in NotificationsConfig; the built-in text was used.", TAckReminder);

            var result = new AckReminderResultDto { Employees = pending.Count };
            foreach (var person in people.OrderBy(x => x.FirstName))
            {
                var name = string.Join(" ", new[] { person.FirstName, person.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
                var to = !string.IsNullOrWhiteSpace(person.EmailID) ? person.EmailID!.Trim() : person.HomeEmail?.Trim();
                if (string.IsNullOrWhiteSpace(to))
                {
                    result.NoEmail++;
                    result.NoEmailEmployees.Add($"{person.EmployeeCode} {name}".Trim());
                    continue;
                }

                var list = pending[person.Id];
                var html = "<ul>" + string.Concat(list.Select(p =>
                    $"<li><b>{System.Net.WebUtility.HtmlEncode(p.Doc.Title)}</b> (version {p.Record.VersionNumber}" +
                    $"{(string.IsNullOrWhiteSpace(p.Record.PeriodLabel) ? "" : ", " + System.Net.WebUtility.HtmlEncode(p.Record.PeriodLabel))}, published {p.Record.UploadedDateTime:dd-MMM-yyyy})</li>")) + "</ul>";
                var values = new Dictionary<string, string>
                {
                    ["EMPLOYEENAME"] = System.Net.WebUtility.HtmlEncode(name),
                    ["EMPLOYEECODE"] = person.EmployeeCode ?? "",
                    ["DOCUMENTLIST"] = html,
                    ["DOCUMENTCOUNT"] = list.Count.ToString(),
                    ["COMPANYNAME"] = System.Net.WebUtility.HtmlEncode(company),
                    ["LINK"] = link
                };
                var replacements = new Dictionary<string, string>();
                foreach (var kv in values)
                {
                    replacements[$"#{kv.Key}#"] = kv.Value;
                    replacements["{{" + kv.Key + "}}"] = kv.Value;
                }
                string Apply(string? text) => replacements.Aggregate(text ?? "", (t, kv) => t.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase));

                string subject, body;
                var processed = config == null ? null : await _notificationConfigService.GetProcessedNotificationContentAsync(config, replacements);
                if (config != null && !string.IsNullOrWhiteSpace(processed) && !string.IsNullOrWhiteSpace(config.EmailSubject))
                {
                    subject = Apply(config.EmailSubject);
                    body = processed!;
                }
                else
                {
                    subject = Apply("Reminder: please read and acknowledge #DOCUMENTCOUNT# document(s)");
                    body = Apply("<p>Dear #EMPLOYEENAME#,</p><p>The following document(s) published by #COMPANYNAME# are waiting for your acknowledgement:</p>#DOCUMENTLIST#" +
                                 "<p>Please log in to the Self Portal, open <b>Documents</b>, read each document and click <b>Viewed and Acknowledged</b>.</p>");
                }

                try
                {
                    if (await EmailService.SendMail(to!, subject, body)) result.Sent++;
                    else result.Failed++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    _logger.LogWarning(ex, "Acknowledgement reminder to {Employee} failed", person.Id);
                }
            }

            foreach (var p in docs)
            {
                var count = pending.Count(kv => kv.Value.Contains(p));
                if (count > 0)
                    AddAudit(p.Doc.IdGeneralDocument, p.Record.IdGeneralDocumentRecord, null, "ACK_REMINDER", new { Employees = count, result.Sent, result.Failed, result.NoEmail }, user);
            }
            await _db.SaveChangesAsync();
            return result;
        }

        #endregion
    }
}
