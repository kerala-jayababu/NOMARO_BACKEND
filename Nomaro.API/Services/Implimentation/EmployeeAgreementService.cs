using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using Document = iText.Layout.Document;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Policy / agreement documents sent to employees for electronic acceptance (click-to-accept with an email
    /// one-time code). Evidence kept: SHA-256 of the exact PDF, every event with time, IP and device in a hash-chained
    /// append-only log, the typed name, and an acceptance certificate emailed to the employee and to HR.
    /// </summary>
    public class EmployeeAgreementService : IEmployeeAgreementService
    {
        private readonly ApplicationDBContext _db;
        private readonly INotificationConfigService _notificationConfigService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmployeeAgreementService> _logger;

        public const string Sent = "SENT", Viewed = "VIEWED", Accepted = "ACCEPTED", Rejected = "REJECTED", Cancelled = "CANCELLED", Expired = "EXPIRED";

        // System parameters
        private const string PHrEmail = "HRDEPARTMENTEMAIL";
        private const string PValidDays = "AGREEMENTLINKVALIDDAYS";
        private const string PPortalUrl = "AGREEMENTPORTALURL";

        // NotificationsConfig.EntityCode of the email templates
        private const string TSend = "AGREEMENTSEND";
        private const string TOtp = "AGREEMENTOTP";
        private const string TEmployeeResponse = "AGREEMENTRESPONSEEMP";
        private const string THrResponse = "AGREEMENTRESPONSEHR";

        private const int OtpValidMinutes = 10;
        private const int OtpMaxAttempts = 5;
        private const int OtpMaxSends = 5;
        private const int OtpResendSeconds = 60;
        private const long MaxFileBytes = 10 * 1024 * 1024;

        public EmployeeAgreementService(ApplicationDBContext db, INotificationConfigService notificationConfigService,
            IConfiguration configuration, IWebHostEnvironment env, ILogger<EmployeeAgreementService> logger)
        {
            _db = db;
            _notificationConfigService = notificationConfigService;
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }

        #region Helpers

        private string StorageFolder(int idAgreement)
        {
            var root = _configuration["EmployeeAgreements:StoragePath"];
            if (string.IsNullOrWhiteSpace(root)) root = System.IO.Path.Combine(_env.ContentRootPath, "SecureUploads", "EmployeeAgreements");
            return System.IO.Path.Combine(root, idAgreement.ToString(CultureInfo.InvariantCulture));
        }

        private static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        private static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));

        private static string NewToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static readonly TimeZoneInfo Ist = FindIst();
        private static TimeZoneInfo FindIst()
        {
            foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* try next */ }
            return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromMinutes(330), "IST", "IST");
        }

        private static string IstText(DateTime? utc) => utc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), Ist).ToString("dd-MMM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) + " IST"
            : "";

        private static string UtcText(DateTime? utc) => utc.HasValue ? utc.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC" : "";

        private static string? Cut(string? v, int max) => v == null ? null : v.Length > max ? v[..max] : v;

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***" + email[Math.Max(at, 0)..];
            return email[..1] + new string('*', Math.Min(at - 1, 6)) + email[at..];
        }

        private async Task<string?> Param(string name) =>
            await _db.SystemParameters.AsNoTracking().Where(p => p.ParameterName == name).Select(p => p.ParameterValue).FirstOrDefaultAsync();

        private async Task<int> ValidDays()
        {
            var v = await Param(PValidDays);
            return int.TryParse(v, out var d) && d is > 0 and <= 365 ? d : 7;
        }

        private async Task<string> PortalLink(string token)
        {
            var baseUrl = (await Param(PPortalUrl))?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException($"System parameter {PPortalUrl} (the web address of the NOMARO UI, e.g. https://hr.company.com) is not set.");
            return $"{baseUrl}/#/agreement/{token}";
        }

        private static string FullName(Employee e) =>
            string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        private static List<string> EmailsOf(Employee e) =>
            new[] { e.EmailID, e.HomeEmail }
                .Where(x => !string.IsNullOrWhiteSpace(x) && Regex.IsMatch(x!.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                .Select(x => x!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        private async Task AddEvent(int idAgreement, string type, string actorType, int? actorId, ClientInfo? client, string? details)
        {
            // previous hash: the last saved event, or one still waiting in this unit of work
            var pending = _db.ChangeTracker.Entries<EmployeeAgreementEvents>()
                .Where(e => e.State == EntityState.Added && e.Entity.IdEmployeeAgreement == idAgreement)
                .Select(e => e.Entity).LastOrDefault();
            var prev = pending?.EventHash ?? await _db.EmployeeAgreementEvents.AsNoTracking()
                .Where(e => e.IdEmployeeAgreement == idAgreement)
                .OrderByDescending(e => e.IdEmployeeAgreementEvent)
                .Select(e => e.EventHash).FirstOrDefaultAsync() ?? "GENESIS";

            var ev = new EmployeeAgreementEvents
            {
                IdEmployeeAgreement = idAgreement,
                EventType = type,
                EventTimeUtc = DateTime.UtcNow,
                ActorType = actorType,
                ActorId = actorId,
                IPAddress = Cut(client?.IPAddress, 64),
                UserAgent = Cut(client?.UserAgent, 500),
                Details = Cut(details, 1000),
                PrevHash = prev
            };
            ev.EventHash = EventHashOf(ev);
            await _db.EmployeeAgreementEvents.AddAsync(ev);
        }

        private static string EventHashOf(EmployeeAgreementEvents ev) => Sha256Hex(string.Join("|",
            ev.PrevHash, ev.IdEmployeeAgreement, ev.EventType,
            DateTime.SpecifyKind(ev.EventTimeUtc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            ev.ActorType, ev.ActorId?.ToString(CultureInfo.InvariantCulture) ?? "", ev.IPAddress ?? "", ev.UserAgent ?? "", ev.Details ?? ""));

        // Email body from NotificationsConfig (EntityCode), with #NAME# / {{NAME}} placeholders; built-in text if missing.
        private async Task<(string subject, string body)> Template(string entityCode, Dictionary<string, string> values, string defaultSubject, string defaultBody)
        {
            var replacements = new Dictionary<string, string>();
            foreach (var kv in values)
            {
                replacements[$"#{kv.Key}#"] = kv.Value;
                replacements["{{" + kv.Key + "}}"] = kv.Value;
            }
            string Apply(string? text) => replacements.Aggregate(text ?? "", (t, kv) => t.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase));

            var config = await _db.NotificationsConfig.AsNoTracking().FirstOrDefaultAsync(n => n.EntityCode == entityCode);
            if (config != null)
            {
                var body = await _notificationConfigService.GetProcessedNotificationContentAsync(config, replacements);
                if (!string.IsNullOrWhiteSpace(body) && !string.IsNullOrWhiteSpace(config.EmailSubject))
                    return (Apply(config.EmailSubject), body);
            }
            _logger.LogWarning("Email template {Code} not found in NotificationsConfig; the built-in text was used.", entityCode);
            return (Apply(defaultSubject), Apply(defaultBody));
        }

        private async Task<string> CompanyName() => (await Param("CompanyName")) ?? "NOMARO HRMS";

        private static string Html(string? text) => System.Net.WebUtility.HtmlEncode(text ?? "");

        #endregion

        #region HR

        public async Task<List<AgreementEmployeeOptionDto>> GetEmployees()
        {
            var list = await _db.Employees.AsNoTracking().OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();
            return list.Select(e => new AgreementEmployeeOptionDto
            {
                IdEmployee = e.IdEmployee ?? 0,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = FullName(e),
                OfficialEmail = e.EmailID,
                PersonalEmail = e.HomeEmail,
                CurrentStatus = e.CurrentStatus
            }).ToList();
        }

        private IQueryable<EmployeeAgreementListDto> ListQuery() =>
            from a in _db.EmployeeAgreements.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on (int?)a.IdEmployee equals e.IdEmployee
            join dep in _db.Departments.AsNoTracking() on e.IdDepartment equals (int?)dep.IdDepartment into deps
            from dep in deps.DefaultIfEmpty()
            join t in _db.DocumentTypes.AsNoTracking() on a.IdDocumentType equals (int?)t.IdDocumentType into ts
            from t in ts.DefaultIfEmpty()
            select new EmployeeAgreementListDto
            {
                IdEmployeeAgreement = a.IdEmployeeAgreement,
                IdEmployee = a.IdEmployee,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = (e.FirstName + " " + (e.LastName ?? "")).Trim(),
                DepartmentName = dep != null ? dep.DepartmentName : null,
                AgreementTitle = a.AgreementTitle,
                AgreementVersion = a.AgreementVersion,
                DocumentTypeName = t != null ? t.DocumentTypeName : null,
                OriginalFileName = a.OriginalFileName,
                Status = a.Status,
                SentTo = a.SentTo,
                SentOnUtc = a.SentOnUtc,
                LinkExpiresOnUtc = a.LinkExpiresOnUtc,
                ViewedOnUtc = a.ViewedOnUtc,
                RespondedOnUtc = a.RespondedOnUtc,
                HasCertificate = a.CertificateFileName != null
            };

        private static void ApplyExpiry(EmployeeAgreementListDto x)
        {
            if ((x.Status == Sent || x.Status == Viewed) && x.LinkExpiresOnUtc.HasValue && x.LinkExpiresOnUtc < DateTime.UtcNow)
                x.Status = Expired;
        }

        public async Task<List<EmployeeAgreementListDto>> GetAgreements(EmployeeAgreementFilterDto filter)
        {
            var q = ListQuery();
            if (filter.IdEmployee is > 0) q = q.Where(x => x.IdEmployee == filter.IdEmployee);
            var now = DateTime.UtcNow;
            switch (filter.Status?.ToUpperInvariant())
            {
                case null or "": break;
                case Expired: q = q.Where(x => (x.Status == Sent || x.Status == Viewed) && x.LinkExpiresOnUtc < now); break;
                case "PENDING": q = q.Where(x => (x.Status == Sent || x.Status == Viewed) && x.LinkExpiresOnUtc >= now); break;
                default: var s = filter.Status.ToUpperInvariant(); q = q.Where(x => x.Status == s); break;
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                foreach (var w in filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
                {
                    var word = w;
                    q = q.Where(x => (x.EmployeeCode != null && x.EmployeeCode.Contains(word)) || x.EmployeeName!.Contains(word) || x.AgreementTitle.Contains(word));
                }
            }
            var list = await q.OrderByDescending(x => x.SentOnUtc).Take(1000).ToListAsync();
            list.ForEach(ApplyExpiry);
            return list;
        }

        public async Task<EmployeeAgreementDetailDto?> GetAgreement(int idAgreement)
        {
            var row = await ListQuery().FirstOrDefaultAsync(x => x.IdEmployeeAgreement == idAgreement);
            if (row == null) return null;
            ApplyExpiry(row);
            var a = await _db.EmployeeAgreements.AsNoTracking().FirstAsync(x => x.IdEmployeeAgreement == idAgreement);
            var events = await _db.EmployeeAgreementEvents.AsNoTracking().Where(e => e.IdEmployeeAgreement == idAgreement)
                .OrderBy(e => e.IdEmployeeAgreementEvent).ToListAsync();

            var actorIds = events.Where(e => e.ActorId.HasValue).Select(e => e.ActorId!.Value).Append(a.SentBy ?? 0).Distinct().ToList();
            var names = await _db.Employees.AsNoTracking().Where(e => e.IdEmployee.HasValue && actorIds.Contains(e.IdEmployee.Value))
                .ToDictionaryAsync(e => e.IdEmployee!.Value, e => (e.FirstName + " " + (e.LastName ?? "")).Trim());

            // check the hash chain
            var intact = true;
            var prev = "GENESIS";
            foreach (var ev in events)
            {
                if (ev.PrevHash != prev || EventHashOf(ev) != ev.EventHash) { intact = false; break; }
                prev = ev.EventHash;
            }

            var dto = new EmployeeAgreementDetailDto
            {
                IdEmployeeAgreement = row.IdEmployeeAgreement, IdEmployee = row.IdEmployee, EmployeeCode = row.EmployeeCode, EmployeeName = row.EmployeeName,
                DepartmentName = row.DepartmentName, AgreementTitle = row.AgreementTitle, AgreementVersion = row.AgreementVersion,
                DocumentTypeName = row.DocumentTypeName, OriginalFileName = row.OriginalFileName, Status = row.Status, SentTo = row.SentTo,
                SentOnUtc = row.SentOnUtc, LinkExpiresOnUtc = row.LinkExpiresOnUtc, ViewedOnUtc = row.ViewedOnUtc, RespondedOnUtc = row.RespondedOnUtc,
                HasCertificate = row.HasCertificate,
                FileHash = a.FileHash, FileSize = a.FileSize, Remarks = a.Remarks, ResponseName = a.ResponseName, ResponseRemarks = a.ResponseRemarks,
                ResponseIPAddress = a.ResponseIPAddress, ResponseUserAgent = a.ResponseUserAgent, CertificateHash = a.CertificateHash,
                CancelReason = a.CancelReason, SentByName = a.SentBy.HasValue && names.TryGetValue(a.SentBy.Value, out var sb) ? sb : null,
                TrailIntact = intact,
                Events = events.Select(e => new EmployeeAgreementEventDto
                {
                    EventType = e.EventType,
                    EventTimeUtc = e.EventTimeUtc,
                    ActorType = e.ActorType,
                    ActorName = e.ActorId.HasValue && names.TryGetValue(e.ActorId.Value, out var n) ? n : null,
                    IPAddress = e.IPAddress,
                    UserAgent = e.UserAgent,
                    Details = e.Details,
                    EventHash = e.EventHash
                }).ToList()
            };
            return dto;
        }

        public async Task<int> SendAgreement(SendEmployeeAgreementDto dto, int hrId, ClientInfo client)
        {
            var title = dto.AgreementTitle?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200) throw new InvalidOperationException("Enter the agreement title (up to 200 characters).");
            if ((dto.AgreementVersion?.Trim().Length ?? 0) > 20) throw new InvalidOperationException("Version must not exceed 20 characters.");
            if ((dto.Remarks?.Length ?? 0) > 500) throw new InvalidOperationException("Remarks must not exceed 500 characters.");
            var file = dto.File ?? throw new InvalidOperationException("Attach the agreement PDF.");
            if (file.Length == 0 || file.Length > MaxFileBytes) throw new InvalidOperationException("The PDF must be between 1 byte and 10 MB.");
            if (!string.Equals(System.IO.Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only PDF files can be sent for acceptance.");

            byte[] bytes;
            await using (var s = file.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            if (bytes.Length < 5 || Encoding.ASCII.GetString(bytes, 0, 5) != "%PDF-") throw new InvalidOperationException("The file is not a valid PDF.");

            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmployee == dto.IdEmployee)
                      ?? throw new InvalidOperationException("Employee not found.");
            var emails = EmailsOf(emp);
            if (!emails.Any()) throw new InvalidOperationException("This employee has no official or personal email address.");
            if (dto.IdDocumentType is > 0 && !await _db.DocumentTypes.AnyAsync(t => t.IdDocumentType == dto.IdDocumentType))
                throw new InvalidOperationException("Select a valid document type.");

            var token = NewToken();
            var link = await PortalLink(token);   // fails early if the portal URL is not configured
            var days = await ValidDays();
            var now = DateTime.UtcNow;

            var agreement = new EmployeeAgreements
            {
                IdEmployee = dto.IdEmployee,
                IdDocumentType = dto.IdDocumentType is > 0 ? dto.IdDocumentType : null,
                AgreementTitle = title,
                AgreementVersion = string.IsNullOrWhiteSpace(dto.AgreementVersion) ? null : dto.AgreementVersion.Trim(),
                OriginalFileName = Cut(System.IO.Path.GetFileName(file.FileName), 255)!,
                StoredFileName = $"{Guid.NewGuid():N}.pdf",
                FileSize = bytes.Length,
                FileHash = Sha256Hex(bytes),
                Status = Sent,
                SentTo = Cut(string.Join(", ", emails), 300),
                SentOnUtc = now,
                SentBy = hrId,
                LinkExpiresOnUtc = now.AddDays(days),
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                CreatedBy = hrId,
                CreatedDateTime = DateTime.Now
            };

            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                await _db.EmployeeAgreements.AddAsync(agreement);
                await _db.SaveChangesAsync();

                var folder = StorageFolder(agreement.IdEmployeeAgreement);
                Directory.CreateDirectory(folder);
                await File.WriteAllBytesAsync(System.IO.Path.Combine(folder, agreement.StoredFileName), bytes);

                await _db.EmployeeAgreementTokens.AddAsync(new EmployeeAgreementTokens
                {
                    IdEmployeeAgreement = agreement.IdEmployeeAgreement,
                    TokenHash = Sha256Hex(token),
                    CreatedOnUtc = now,
                    ExpiresOnUtc = agreement.LinkExpiresOnUtc.Value
                });
                await AddEvent(agreement.IdEmployeeAgreement, "CREATED", "HR", hrId, client,
                    $"File {agreement.OriginalFileName}, {bytes.Length} bytes, SHA-256 {agreement.FileHash}");
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }

            await EmailAgreement(agreement, emp, emails, token, link, hrId, client, "SENT");
            return agreement.IdEmployeeAgreement;
        }

        private async Task EmailAgreement(EmployeeAgreements a, Employee emp, List<string> emails, string token, string link, int hrId, ClientInfo client, string eventType)
        {
            var values = new Dictionary<string, string>
            {
                ["EMPLOYEENAME"] = Html(FullName(emp)),
                ["EMPLOYEECODE"] = Html(emp.EmployeeCode),
                ["AGREEMENTTITLE"] = Html(a.AgreementTitle),
                ["AGREEMENTVERSION"] = Html(a.AgreementVersion ?? ""),
                ["LINK"] = link,
                ["EXPIRYDATE"] = IstText(a.LinkExpiresOnUtc),
                ["DOCUMENTHASH"] = a.FileHash,
                ["FILENAME"] = Html(a.OriginalFileName),
                ["COMPANYNAME"] = Html(await CompanyName())
            };
            var (subject, body) = await Template(TSend, values,
                "Please review and accept: #AGREEMENTTITLE#",
                "<p>Dear #EMPLOYEENAME#,</p><p>#COMPANYNAME# has sent you <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# for your review and acceptance. The document is attached to this email.</p>" +
                "<p><a href=\"#LINK#\" style=\"display:inline-block;padding:10px 18px;background:#0d384d;color:#fff;text-decoration:none;border-radius:6px\">Review Agreement</a></p>" +
                "<p>The link is valid until #EXPIRYDATE#. On the review page you can accept or reject the agreement; a one-time code will be sent to this email address to confirm it is you.</p>" +
                "<p style=\"color:#666;font-size:12px\">Document: #FILENAME#<br>SHA-256 fingerprint: #DOCUMENTHASH#<br>If you did not expect this email, please contact HR.</p>");

            var pdf = await File.ReadAllBytesAsync(System.IO.Path.Combine(StorageFolder(a.IdEmployeeAgreement), a.StoredFileName));
            var sentTo = new List<string>();
            var failed = new List<string>();
            foreach (var to in emails)
            {
                try { await EmailService.SendMail(to, subject, body, pdf, a.OriginalFileName); sentTo.Add(to); }
                catch (Exception ex) { failed.Add(to); _logger.LogError(ex, "Agreement {Id}: email to {To} failed.", a.IdEmployeeAgreement, to); }
            }

            if (sentTo.Any())
                await AddEvent(a.IdEmployeeAgreement, eventType, "HR", hrId, client,
                    $"Emailed to {string.Join(", ", sentTo)} with the PDF attached. Link valid until {UtcText(a.LinkExpiresOnUtc)}.");
            if (failed.Any())
                await AddEvent(a.IdEmployeeAgreement, "EMAIL_FAILED", "SYSTEM", null, client, $"Could not email {string.Join(", ", failed)}.");
            await _db.SaveChangesAsync();

            if (!sentTo.Any())
                throw new InvalidOperationException("The agreement was saved, but the email could not be sent. Check the email settings and use Resend.");
        }

        public async Task ResendAgreement(int idAgreement, int hrId, ClientInfo client)
        {
            var a = await _db.EmployeeAgreements.FirstOrDefaultAsync(x => x.IdEmployeeAgreement == idAgreement)
                    ?? throw new KeyNotFoundException("Agreement not found.");
            if (a.Status != Sent && a.Status != Viewed)
                throw new InvalidOperationException($"This agreement is {a.Status.ToLowerInvariant()} and cannot be resent.");
            var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == a.IdEmployee);
            var emails = EmailsOf(emp);
            if (!emails.Any()) throw new InvalidOperationException("This employee has no official or personal email address.");

            var token = NewToken();
            var link = await PortalLink(token);
            var now = DateTime.UtcNow;
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                var old = await _db.EmployeeAgreementTokens.Where(t => t.IdEmployeeAgreement == idAgreement && !t.IsRevoked).ToListAsync();
                old.ForEach(t => t.IsRevoked = true);
                a.LinkExpiresOnUtc = now.AddDays(await ValidDays());
                a.SentTo = Cut(string.Join(", ", emails), 300);
                a.ModifiedBy = hrId;
                a.ModifiedDateTime = DateTime.Now;
                await _db.EmployeeAgreementTokens.AddAsync(new EmployeeAgreementTokens
                {
                    IdEmployeeAgreement = idAgreement,
                    TokenHash = Sha256Hex(token),
                    CreatedOnUtc = now,
                    ExpiresOnUtc = a.LinkExpiresOnUtc.Value
                });
                await AddEvent(idAgreement, "LINK_REPLACED", "HR", hrId, client, $"{old.Count} earlier link(s) disabled.");
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            await EmailAgreement(a, emp, emails, token, link, hrId, client, "RESENT");
        }

        public async Task CancelAgreement(int idAgreement, string? reason, int hrId, ClientInfo client)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Enter the reason for cancelling.");
            var a = await _db.EmployeeAgreements.FirstOrDefaultAsync(x => x.IdEmployeeAgreement == idAgreement)
                    ?? throw new KeyNotFoundException("Agreement not found.");
            if (a.Status != Sent && a.Status != Viewed)
                throw new InvalidOperationException($"This agreement is {a.Status.ToLowerInvariant()} and cannot be cancelled.");
            var tokens = await _db.EmployeeAgreementTokens.Where(t => t.IdEmployeeAgreement == idAgreement && !t.IsRevoked).ToListAsync();
            tokens.ForEach(t => t.IsRevoked = true);
            a.Status = Cancelled;
            a.CancelReason = Cut(reason.Trim(), 500);
            a.ModifiedBy = hrId;
            a.ModifiedDateTime = DateTime.Now;
            await AddEvent(idAgreement, "CANCELLED", "HR", hrId, client, a.CancelReason);
            await _db.SaveChangesAsync();
        }

        public async Task<FileDownloadDto?> GetAgreementFile(int idAgreement)
        {
            var a = await _db.EmployeeAgreements.AsNoTracking().FirstOrDefaultAsync(x => x.IdEmployeeAgreement == idAgreement);
            if (a == null) return null;
            var path = System.IO.Path.Combine(StorageFolder(idAgreement), a.StoredFileName);
            return File.Exists(path) ? new FileDownloadDto { Content = await File.ReadAllBytesAsync(path), ContentType = "application/pdf", FileName = a.OriginalFileName } : null;
        }

        public async Task<FileDownloadDto?> GetCertificateFile(int idAgreement)
        {
            var a = await _db.EmployeeAgreements.AsNoTracking().FirstOrDefaultAsync(x => x.IdEmployeeAgreement == idAgreement);
            if (a?.CertificateFileName == null) return null;
            var path = System.IO.Path.Combine(StorageFolder(idAgreement), a.CertificateFileName);
            return File.Exists(path)
                ? new FileDownloadDto { Content = await File.ReadAllBytesAsync(path), ContentType = "application/pdf", FileName = $"Certificate_{a.IdEmployeeAgreement}_{a.Status}.pdf" }
                : null;
        }

        #endregion

        #region Public review page

        private async Task<(EmployeeAgreementTokens? token, EmployeeAgreements? agreement, string state)> Resolve(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return (null, null, "INVALID");
            var hash = Sha256Hex(token.Trim());
            var t = await _db.EmployeeAgreementTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (t == null) return (null, null, "INVALID");
            var a = await _db.EmployeeAgreements.FirstAsync(x => x.IdEmployeeAgreement == t.IdEmployeeAgreement);
            if (a.Status == Accepted || a.Status == Rejected || a.Status == Cancelled) return (t, a, a.Status);
            if (t.IsRevoked) return (t, a, "REPLACED");
            if (t.ExpiresOnUtc < DateTime.UtcNow) return (t, a, Expired);
            return (t, a, "OPEN");
        }

        public async Task<AgreementPortalViewDto> GetPortalView(string token, ClientInfo client)
        {
            var (t, a, state) = await Resolve(token);
            var view = new AgreementPortalViewDto { State = state, OtpValidMinutes = OtpValidMinutes, CompanyName = await CompanyName() };
            if (a == null) return view;

            var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == a.IdEmployee);
            view.EmployeeName = FullName(emp);
            view.AgreementTitle = a.AgreementTitle;
            view.AgreementVersion = a.AgreementVersion;
            view.OriginalFileName = a.OriginalFileName;
            view.FileHash = a.FileHash;
            view.SentOnUtc = a.SentOnUtc;
            view.LinkExpiresOnUtc = t!.ExpiresOnUtc;
            view.RespondedOnUtc = a.RespondedOnUtc;
            view.MaskedEmails = EmailsOf(emp).Select(MaskEmail).ToList();

            if (state == "OPEN")
            {
                await AddEvent(a.IdEmployeeAgreement, "LINK_OPENED", "EMPLOYEE", a.IdEmployee, client, null);
                await _db.SaveChangesAsync();
            }
            return view;
        }

        public async Task<FileDownloadDto?> GetPortalDocument(string token, ClientInfo client)
        {
            var (_, a, state) = await Resolve(token);
            if (a == null || state == "INVALID" || state == "REPLACED") return null;
            var file = await GetAgreementFile(a.IdEmployeeAgreement);
            if (file == null) return null;

            if (state == "OPEN")
            {
                if (a.ViewedOnUtc == null)
                {
                    a.ViewedOnUtc = DateTime.UtcNow;
                    a.Status = Viewed;
                }
                await AddEvent(a.IdEmployeeAgreement, "DOCUMENT_VIEWED", "EMPLOYEE", a.IdEmployee, client, $"SHA-256 {Sha256Hex(file.Content)}");
                await _db.SaveChangesAsync();
            }
            return file;
        }

        public async Task<string> SendOtp(string token, ClientInfo client)
        {
            var (t, a, state) = await Resolve(token);
            if (state != "OPEN") throw new InvalidOperationException(StateMessage(state));
            if (t!.OtpSentOnUtc.HasValue && (DateTime.UtcNow - t.OtpSentOnUtc.Value).TotalSeconds < OtpResendSeconds)
                throw new InvalidOperationException($"Please wait a minute before asking for another code.");
            if (t.OtpSentCount >= OtpMaxSends)
                throw new InvalidOperationException("Too many codes have been requested for this link. Please contact HR for a new link.");

            var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == a!.IdEmployee);
            var emails = EmailsOf(emp);
            if (!emails.Any()) throw new InvalidOperationException("No email address is available. Please contact HR.");

            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var (subject, body) = await Template(TOtp, new Dictionary<string, string>
            {
                ["EMPLOYEENAME"] = Html(FullName(emp)),
                ["OTP"] = otp,
                ["OTPMINUTES"] = OtpValidMinutes.ToString(CultureInfo.InvariantCulture),
                ["AGREEMENTTITLE"] = Html(a!.AgreementTitle),
                ["COMPANYNAME"] = Html(await CompanyName())
            },
            "Your confirmation code: #OTP#",
            "<p>Dear #EMPLOYEENAME#,</p><p>Your one-time code to respond to <b>#AGREEMENTTITLE#</b> is:</p>" +
            "<p style=\"font-size:24px;font-weight:bold;letter-spacing:4px\">#OTP#</p><p>The code is valid for #OTPMINUTES# minutes. Do not share it with anyone.</p>");

            var sent = new List<string>();
            foreach (var to in emails)
            {
                try { await EmailService.SendMail(to, subject, body); sent.Add(to); }
                catch (Exception ex) { _logger.LogError(ex, "Agreement {Id}: OTP email to {To} failed.", a.IdEmployeeAgreement, to); }
            }
            if (!sent.Any()) throw new InvalidOperationException("The code could not be emailed. Please try again later.");

            t.OtpHash = Sha256Hex($"{t.TokenHash}:{otp}");
            t.OtpExpiresOnUtc = DateTime.UtcNow.AddMinutes(OtpValidMinutes);
            t.OtpAttempts = 0;
            t.OtpSentCount += 1;
            t.OtpSentOnUtc = DateTime.UtcNow;
            await AddEvent(a.IdEmployeeAgreement, "OTP_SENT", "SYSTEM", null, client, $"Code emailed to {string.Join(", ", sent)}.");
            await _db.SaveChangesAsync();
            return $"A 6-digit code has been sent to {string.Join(" and ", sent.Select(MaskEmail))}.";
        }

        private static string StateMessage(string state) => state switch
        {
            Accepted => "This agreement has already been accepted.",
            Rejected => "This agreement has already been rejected.",
            Cancelled => "This agreement was withdrawn by HR.",
            Expired => "This link has expired. Please contact HR for a new link.",
            "REPLACED" => "A newer link has been sent to you. Please use the latest email.",
            _ => "This link is not valid."
        };

        private static string NormaliseName(string? s) => Regex.Replace((s ?? "").Trim().ToLowerInvariant(), @"\s+", " ");

        public async Task<AgreementPortalViewDto> Respond(AgreementResponseDto dto, ClientInfo client)
        {
            var decision = (dto.Decision ?? "").Trim().ToUpperInvariant();
            if (decision != "ACCEPT" && decision != "REJECT") throw new InvalidOperationException("Choose Accept or Reject.");

            var (t, a, state) = await Resolve(dto.Token);
            if (state != "OPEN") throw new InvalidOperationException(StateMessage(state));
            if (a!.ViewedOnUtc == null) throw new InvalidOperationException("Open and read the document before responding.");
            if (decision == "ACCEPT" && !dto.ConfirmRead) throw new InvalidOperationException("Tick the box to confirm you have read and understood the agreement.");
            if (decision == "REJECT" && string.IsNullOrWhiteSpace(dto.Remarks)) throw new InvalidOperationException("Enter the reason for rejecting.");
            if ((dto.Remarks?.Length ?? 0) > 500) throw new InvalidOperationException("The reason must not exceed 500 characters.");

            var emp = await _db.Employees.AsNoTracking().FirstAsync(e => e.IdEmployee == a.IdEmployee);
            var typed = NormaliseName(dto.FullName);
            var full = NormaliseName(FullName(emp));
            var shortName = NormaliseName($"{emp.FirstName} {emp.LastName}");
            if (typed.Length == 0 || (typed != full && typed != shortName))
                throw new InvalidOperationException($"Type your full name exactly as in your employee record: {FullName(emp)}.");

            // one-time code
            if (t!.OtpHash == null || t.OtpExpiresOnUtc < DateTime.UtcNow)
                throw new InvalidOperationException("The code has expired or was not requested. Click Send Code to get a new one.");
            if (t.OtpAttempts >= OtpMaxAttempts)
                throw new InvalidOperationException("Too many wrong codes. Click Send Code to get a new one.");
            var otpOk = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Sha256Hex($"{t.TokenHash}:{(dto.Otp ?? "").Trim()}")), Encoding.ASCII.GetBytes(t.OtpHash));
            if (!otpOk)
            {
                t.OtpAttempts += 1;
                await AddEvent(a.IdEmployeeAgreement, "OTP_FAILED", "EMPLOYEE", a.IdEmployee, client, $"Attempt {t.OtpAttempts} of {OtpMaxAttempts}.");
                await _db.SaveChangesAsync();
                throw new InvalidOperationException($"The code is not correct. {OtpMaxAttempts - t.OtpAttempts} attempt(s) left.");
            }

            var now = DateTime.UtcNow;
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                // re-read inside the transaction so two clicks cannot both succeed
                await _db.Entry(a).ReloadAsync();
                if (a.Status != Sent && a.Status != Viewed) throw new InvalidOperationException(StateMessage(a.Status));

                a.Status = decision == "ACCEPT" ? Accepted : Rejected;
                a.RespondedOnUtc = now;
                a.ResponseName = Cut(dto.FullName.Trim(), 150);
                a.ResponseRemarks = Cut(dto.Remarks?.Trim(), 500);
                a.ResponseIPAddress = Cut(client.IPAddress, 64);
                a.ResponseUserAgent = Cut(client.UserAgent, 500);
                t.IsRevoked = true;
                t.OtpHash = null;

                await AddEvent(a.IdEmployeeAgreement, "OTP_VERIFIED", "EMPLOYEE", a.IdEmployee, client, null);
                await AddEvent(a.IdEmployeeAgreement, a.Status, "EMPLOYEE", a.IdEmployee, client,
                    $"Typed name: {a.ResponseName}. Confirmed read: {(dto.ConfirmRead ? "yes" : "no")}. Document SHA-256 {a.FileHash}."
                    + (string.IsNullOrWhiteSpace(a.ResponseRemarks) ? "" : $" Remarks: {a.ResponseRemarks}"));
                await _db.SaveChangesAsync();

                // certificate includes every event up to the decision
                var cert = await BuildCertificate(a, emp);
                a.CertificateFileName = $"certificate_{Guid.NewGuid():N}.pdf";
                a.CertificateHash = Sha256Hex(cert);
                await File.WriteAllBytesAsync(System.IO.Path.Combine(StorageFolder(a.IdEmployeeAgreement), a.CertificateFileName), cert);
                await AddEvent(a.IdEmployeeAgreement, "CERTIFICATE_CREATED", "SYSTEM", null, null, $"SHA-256 {a.CertificateHash}");
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }

            await SendResponseEmails(a, emp);

            return new AgreementPortalViewDto
            {
                State = a.Status,
                CompanyName = await CompanyName(),
                EmployeeName = FullName(emp),
                AgreementTitle = a.AgreementTitle,
                AgreementVersion = a.AgreementVersion,
                OriginalFileName = a.OriginalFileName,
                FileHash = a.FileHash,
                SentOnUtc = a.SentOnUtc,
                RespondedOnUtc = a.RespondedOnUtc,
                MaskedEmails = EmailsOf(emp).Select(MaskEmail).ToList()
            };
        }

        private async Task SendResponseEmails(EmployeeAgreements a, Employee emp)
        {
            var cert = await GetCertificateFile(a.IdEmployeeAgreement);
            var decisionWord = a.Status == Accepted ? "ACCEPTED" : "REJECTED";
            var values = new Dictionary<string, string>
            {
                ["EMPLOYEENAME"] = Html(FullName(emp)),
                ["EMPLOYEECODE"] = Html(emp.EmployeeCode),
                ["AGREEMENTTITLE"] = Html(a.AgreementTitle),
                ["AGREEMENTVERSION"] = Html(a.AgreementVersion ?? ""),
                ["DECISION"] = decisionWord,
                ["RESPONDEDON"] = IstText(a.RespondedOnUtc),
                ["REMARKS"] = Html(a.ResponseRemarks ?? ""),
                ["IPADDRESS"] = Html(a.ResponseIPAddress ?? ""),
                ["DOCUMENTHASH"] = a.FileHash,
                ["CERTIFICATEHASH"] = a.CertificateHash ?? "",
                ["COMPANYNAME"] = Html(await CompanyName())
            };

            // employee
            var (es, eb) = await Template(TEmployeeResponse, values,
                "You have #DECISION# #AGREEMENTTITLE#",
                "<p>Dear #EMPLOYEENAME#,</p><p>This confirms that you <b>#DECISION#</b> <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# on #RESPONDEDON#.</p>" +
                "<p>The acceptance certificate is attached. Please keep this email for your records.</p>" +
                "<p style=\"color:#666;font-size:12px\">Document SHA-256: #DOCUMENTHASH#<br>Certificate SHA-256: #CERTIFICATEHASH#</p>");
            var sentEmp = new List<string>();
            foreach (var to in EmailsOf(emp))
            {
                try
                {
                    if (cert != null) await EmailService.SendMail(to, es, eb, cert.Content, cert.FileName);
                    else await EmailService.SendMail(to, es, eb);
                    sentEmp.Add(to);
                }
                catch (Exception ex) { _logger.LogError(ex, "Agreement {Id}: confirmation to {To} failed.", a.IdEmployeeAgreement, to); }
            }
            await AddEvent(a.IdEmployeeAgreement, sentEmp.Any() ? "EMPLOYEE_CONFIRMED" : "EMAIL_FAILED", "SYSTEM", null, null,
                sentEmp.Any() ? $"Confirmation and certificate emailed to {string.Join(", ", sentEmp)}." : "Confirmation email to the employee failed.");

            // HR department
            var hrEmail = (await Param(PHrEmail))?.Trim();
            if (string.IsNullOrWhiteSpace(hrEmail))
            {
                await AddEvent(a.IdEmployeeAgreement, "EMAIL_FAILED", "SYSTEM", null, null, $"System parameter {PHrEmail} is not set; HR was not emailed.");
            }
            else
            {
                var (hs, hb) = await Template(THrResponse, values,
                    "#EMPLOYEECODE# #EMPLOYEENAME# #DECISION# #AGREEMENTTITLE#",
                    "<p>#EMPLOYEECODE# #EMPLOYEENAME# has <b>#DECISION#</b> <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# on #RESPONDEDON# (IP #IPADDRESS#).</p>" +
                    "<p>Remarks: #REMARKS#</p><p>The certificate with the full evidence trail is attached.</p>" +
                    "<p style=\"color:#666;font-size:12px\">Document SHA-256: #DOCUMENTHASH#<br>Certificate SHA-256: #CERTIFICATEHASH#</p>");
                var sentHr = new List<string>();
                foreach (var to in hrEmail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    try
                    {
                        if (cert != null) await EmailService.SendMail(to, hs, hb, cert.Content, cert.FileName);
                        else await EmailService.SendMail(to, hs, hb);
                        sentHr.Add(to);
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Agreement {Id}: HR email to {To} failed.", a.IdEmployeeAgreement, to); }
                }
                await AddEvent(a.IdEmployeeAgreement, sentHr.Any() ? "HR_NOTIFIED" : "EMAIL_FAILED", "SYSTEM", null, null,
                    sentHr.Any() ? $"HR emailed at {string.Join(", ", sentHr)} with the certificate." : $"Email to HR ({hrEmail}) failed.");
            }
            await _db.SaveChangesAsync();
        }

        #endregion

        #region Certificate

        private async Task<byte[]> BuildCertificate(EmployeeAgreements a, Employee emp)
        {
            var events = await _db.EmployeeAgreementEvents.AsNoTracking().Where(e => e.IdEmployeeAgreement == a.IdEmployeeAgreement)
                .OrderBy(e => e.IdEmployeeAgreementEvent).ToListAsync();
            var company = await CompanyName();
            var sentBy = a.SentBy.HasValue
                ? await _db.Employees.AsNoTracking().Where(e => e.IdEmployee == a.SentBy).Select(e => (e.EmployeeCode + " " + e.FirstName + " " + (e.LastName ?? "")).Trim()).FirstOrDefaultAsync()
                : null;

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(36, 36, 36, 36);
                var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var grey = new DeviceRgb(110, 110, 110);
                var accent = new DeviceRgb(13, 56, 77);
                pdf.GetDocumentInfo().SetTitle($"Electronic {(a.Status == Accepted ? "Acceptance" : "Rejection")} Certificate").SetCreator("NOMARO HRMS");

                doc.Add(new Paragraph(company).SetFont(bold).SetFontSize(11).SetFontColor(grey));
                doc.Add(new Paragraph($"Electronic {(a.Status == Accepted ? "Acceptance" : "Rejection")} Certificate").SetFont(bold).SetFontSize(18).SetFontColor(accent).SetMarginBottom(2));
                doc.Add(new Paragraph($"Agreement reference AGR-{a.IdEmployeeAgreement:D6}").SetFont(regular).SetFontSize(9).SetFontColor(grey));

                var summary = new Table(UnitValue.CreatePercentArray(new float[] { 32, 68 })).UseAllAvailableWidth().SetMarginTop(10);
                void Row(string k, string? v)
                {
                    summary.AddCell(new Cell().Add(new Paragraph(k).SetFont(bold).SetFontSize(9)).SetBackgroundColor(new DeviceRgb(243, 245, 247)));
                    summary.AddCell(new Cell().Add(new Paragraph(string.IsNullOrWhiteSpace(v) ? "-" : v).SetFont(regular).SetFontSize(9)));
                }
                Row("Decision", a.Status == Accepted ? "ACCEPTED" : "REJECTED");
                Row("Employee", $"{emp.EmployeeCode} - {FullName(emp)}");
                Row("Name typed by employee", a.ResponseName);
                Row("Responded on", $"{IstText(a.RespondedOnUtc)}  ({UtcText(a.RespondedOnUtc)})");
                Row("IP address", a.ResponseIPAddress);
                Row("Browser / device", a.ResponseUserAgent);
                Row("Identity check", "One-time code emailed to the employee's registered email address(es) and entered correctly");
                if (!string.IsNullOrWhiteSpace(a.ResponseRemarks)) Row("Employee remarks", a.ResponseRemarks);
                Row("Agreement", $"{a.AgreementTitle}{(string.IsNullOrWhiteSpace(a.AgreementVersion) ? "" : " (version " + a.AgreementVersion + ")")}");
                Row("File", $"{a.OriginalFileName} ({a.FileSize:N0} bytes)");
                Row("Document SHA-256", a.FileHash);
                Row("Sent to", a.SentTo);
                Row("Sent on / by", $"{IstText(a.SentOnUtc)} by {sentBy ?? "HR"}");
                doc.Add(summary);

                doc.Add(new Paragraph("Evidence trail").SetFont(bold).SetFontSize(12).SetFontColor(accent).SetMarginTop(14));
                doc.Add(new Paragraph("Each entry stores the SHA-256 hash of the previous entry; any change to an entry breaks the chain.")
                    .SetFont(regular).SetFontSize(8).SetFontColor(grey));
                var trail = new Table(UnitValue.CreatePercentArray(new float[] { 22, 17, 13, 48 })).UseAllAvailableWidth();
                foreach (var h in new[] { "Time (IST)", "Event", "IP address", "Details / entry hash" })
                    trail.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(8)).SetBackgroundColor(new DeviceRgb(243, 245, 247)));
                foreach (var e in events)
                {
                    trail.AddCell(new Cell().Add(new Paragraph(IstText(e.EventTimeUtc)).SetFont(regular).SetFontSize(7.5f)));
                    trail.AddCell(new Cell().Add(new Paragraph(e.EventType).SetFont(regular).SetFontSize(7.5f)));
                    trail.AddCell(new Cell().Add(new Paragraph(e.IPAddress ?? "").SetFont(regular).SetFontSize(7.5f)));
                    trail.AddCell(new Cell().Add(new Paragraph((e.Details ?? "") + "\n" + e.EventHash).SetFont(regular).SetFontSize(7f)));
                }
                doc.Add(trail);

                doc.Add(new Paragraph(
                    $"This certificate was generated automatically by NOMARO HRMS for {company} on {IstText(DateTime.UtcNow)}. " +
                    "It records the electronic response of the employee named above to the document identified by its SHA-256 fingerprint. " +
                    "The fingerprint of any copy of the document can be checked against the value above to confirm it is the same document.")
                    .SetFont(regular).SetFontSize(8).SetFontColor(grey).SetMarginTop(12));
            }
            return ms.ToArray();
        }

        #endregion
    }
}
