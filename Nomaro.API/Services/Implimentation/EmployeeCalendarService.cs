using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Birthdays, work anniversaries (with congratulations emails) and the holiday calendar.
    /// Birthdays use Employees.DateOfBirth and anniversaries Employees.JoiningDate; only day and month are compared.
    /// </summary>
    public class EmployeeCalendarService : IEmployeeCalendarService
    {
        public const string OccasionBirthday = "BIRTHDAY";
        public const string OccasionWorkAnniversary = "WORKANNIVERSARY";

        // NotificationsConfig.EntityCode of the email templates
        private const string BirthdayEntityCode = "BIRTHDAYMESSAGE";
        private const string WorkAnniversaryEntityCode = "WORKDAYMESSAGE";

        private const string WorkingStatus = "Working";
        private const int MaxUpcomingDays = 31;

        private readonly ApplicationDBContext _dbContext;
        private readonly INotificationConfigService _notificationConfigService;
        private readonly ILogger<EmployeeCalendarService> _logger;

        public EmployeeCalendarService(
            ApplicationDBContext dbContext,
            INotificationConfigService notificationConfigService,
            ILogger<EmployeeCalendarService> logger)
        {
            _dbContext = dbContext;
            _notificationConfigService = notificationConfigService;
            _logger = logger;
        }

        // ───────────── Birthdays & work anniversaries ─────────────

        public async Task<CelebrationsDto> GetCelebrations(int upcomingDays)
        {
            try
            {
                upcomingDays = Math.Clamp(upcomingDays, 1, MaxUpcomingDays);
                var today = DateTime.Today;

                var employees = await LoadWorkingEmployees();
                var birthdays = new List<CelebrationEmployeeDto>();
                var anniversaries = new List<CelebrationEmployeeDto>();

                foreach (var e in employees)
                {
                    if (e.DateOfBirth.HasValue)
                    {
                        var occasion = NextOccurrence(e.DateOfBirth.Value, today);
                        var days = (occasion - today).Days;
                        if (days <= upcomingDays)
                            birthdays.Add(ToDto(e, occasion, days));
                    }

                    if (e.JoiningDate.HasValue)
                    {
                        var occasion = NextOccurrence(e.JoiningDate.Value, today);
                        var days = (occasion - today).Days;
                        var years = occasion.Year - e.JoiningDate.Value.Year;
                        // Only completed years (no "0 years" on the joining day itself)
                        if (days <= upcomingDays && years >= 1)
                        {
                            var dto = ToDto(e, occasion, days);
                            dto.JoiningDate = e.JoiningDate.Value.Date;
                            dto.YearsOfService = years;
                            anniversaries.Add(dto);
                        }
                    }
                }

                // Photos and email status only for the employees shown
                var shown = birthdays.Concat(anniversaries).ToList();
                await LoadPhotos(shown, employees);
                await LoadEmailStatus(birthdays, BirthdayEntityCode);
                await LoadEmailStatus(anniversaries, WorkAnniversaryEntityCode);

                static List<CelebrationEmployeeDto> Sort(IEnumerable<CelebrationEmployeeDto> list) =>
                    list.OrderBy(x => x.DaysAway).ThenBy(x => x.EmployeeName).ToList();

                return new CelebrationsDto
                {
                    Today = today,
                    UpcomingDays = upcomingDays,
                    TodaysBirthdays = Sort(birthdays.Where(x => x.DaysAway == 0)),
                    UpcomingBirthdays = Sort(birthdays.Where(x => x.DaysAway > 0)),
                    TodaysWorkAnniversaries = Sort(anniversaries.Where(x => x.DaysAway == 0)),
                    UpcomingWorkAnniversaries = Sort(anniversaries.Where(x => x.DaysAway > 0)),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching birthdays and work anniversaries.");
                throw;
            }
        }

        /// <summary>
        /// Sends the congratulations email for today's birthday / work anniversary using the
        /// NotificationsConfig template, and records it in Notifications (also stops a second send).
        /// Returns the time it was sent.
        /// </summary>
        public async Task<DateTime> SendCongratulationsEmail(SendCongratulationsDto dto, int idLoggedInEmployee)
        {
            var occasionType = (dto?.Occasion ?? string.Empty).Trim().ToUpperInvariant();
            if (occasionType != OccasionBirthday && occasionType != OccasionWorkAnniversary)
                throw new InvalidOperationException("Occasion must be BIRTHDAY or WORKANNIVERSARY.");

            var today = DateTime.Today;
            var employee = (await LoadWorkingEmployees(dto!.IdEmployee)).FirstOrDefault();
            if (employee == null)
                throw new InvalidOperationException("Employee not found or not working.");

            var isBirthday = occasionType == OccasionBirthday;
            var baseDate = isBirthday ? employee.DateOfBirth : employee.JoiningDate;
            if (!baseDate.HasValue || NextOccurrence(baseDate.Value, today) != today)
                throw new InvalidOperationException(isBirthday
                    ? $"Today is not {employee.EmployeeName}'s birthday."
                    : $"Today is not {employee.EmployeeName}'s work anniversary.");

            var yearsOfService = isBirthday ? (int?)null : today.Year - baseDate.Value.Year;
            if (!isBirthday && yearsOfService < 1)
                throw new InvalidOperationException($"{employee.EmployeeName} joined today; there is no anniversary yet.");

            if (string.IsNullOrWhiteSpace(employee.EmailID))
                throw new InvalidOperationException($"{employee.EmployeeName} does not have an email address.");

            var entityCode = isBirthday ? BirthdayEntityCode : WorkAnniversaryEntityCode;
            var config = await _dbContext.NotificationsConfig.AsNoTracking()
                .FirstOrDefaultAsync(n => n.EntityCode == entityCode);
            if (config == null)
                throw new InvalidOperationException($"Email template {entityCode} is not set up in Notification Types.");
            if (config.EmailNotificationEnabled == false)
                throw new InvalidOperationException($"Email is turned off for the {entityCode} template.");

            // One congratulations email per employee per occasion per year
            var yearStart = new DateTime(today.Year, 1, 1);
            var alreadySent = await _dbContext.Notifications.AsNoTracking()
                .Where(n => n.ReceivedByIdEmployee == employee.IdEmployee && n.RelatedRecordType == entityCode && n.CreatedAt >= yearStart)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => (DateTime?)n.CreatedAt)
                .FirstOrDefaultAsync();
            if (alreadySent.HasValue)
                throw new InvalidOperationException($"Already sent to {employee.EmployeeName} on {alreadySent.Value:dd-MMM-yyyy hh:mm tt}.");

            var replacements = await BuildReplacements(employee, yearsOfService);
            var subject = ApplyReplacements(config.EmailSubject, replacements);
            var content = await _notificationConfigService.GetProcessedNotificationContentAsync(config, replacements);
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"Email subject or content is empty in the {entityCode} template.");

            await EmailService.SendMail(employee.EmailID!, subject, content);

            var sentAt = DateTime.Now;

            // Record the send in Notifications (used to block a second send). The full HTML body is not stored:
            // Notifications.EmailContent is too short for a complete HTML email.
            try
            {
                _dbContext.Notifications.Add(new Notification
                {
                    IdNotificationConfig = config.IdNotificationConfig,
                    NotificationType = config.NotificationType,
                    SentByIdEmployee = idLoggedInEmployee,
                    ReceivedByIdEmployee = employee.IdEmployee,
                    EmailSubject = Shorten(subject, 200),
                    EmailContent = null,
                    EmailSentStatus = "SENT",
                    AppNotificationText = Shorten(ApplyReplacements(config.AppNotificationText, replacements), 250),
                    IsReadAppNotification = false,
                    CreatedAt = sentAt,
                    Status = "SENT",
                    RelatedRecordID = employee.IdEmployee,
                    RelatedRecordType = entityCode,
                    LogoText = config.LogoText
                });
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // The email has already gone out - do not report a failure (that would invite a second send)
                _logger.LogError(ex, "{EntityCode} email sent to employee {IdEmployee} but recording it in Notifications failed", entityCode, employee.IdEmployee);
            }

            _logger.LogInformation("{EntityCode} email sent to employee {IdEmployee} by {SentBy}", entityCode, employee.IdEmployee, idLoggedInEmployee);
            return sentAt;
        }

        // ───────────── Holiday calendar ─────────────

        public async Task<IEnumerable<HolidayCalendarDto>> GetHolidayCalendar(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var startDay = fromDate.Date;
                var endExclusive = toDate.Date.AddDays(1); // include the whole last day
                return await (
                        from h in _dbContext.Holidays.AsNoTracking()
                        join t in _dbContext.HolidayTypes.AsNoTracking() on h.HolidayType equals t.HolidayType into tj
                        from t in tj.DefaultIfEmpty()
                        where h.HolidayDate >= startDay && h.HolidayDate < endExclusive
                        orderby h.HolidayDate
                        select new HolidayCalendarDto
                        {
                            IdHoliday = h.IdHoliday,
                            HolidayDate = h.HolidayDate,
                            HolidayType = h.HolidayType,
                            HolidayTypeName = t != null ? t.HolidayTypeName : h.HolidayType,
                            HolidayDescription = h.HolidayDescription
                        })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holidays from {From} to {To}", fromDate, toDate);
                throw;
            }
        }

        // ───────────── helpers ─────────────

        private sealed class EmployeeRow
        {
            public int IdEmployee { get; set; }
            public string? EmployeeCode { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? EmailID { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public DateTime? JoiningDate { get; set; }
            public string? DepartmentName { get; set; }
            public string? DesignationName { get; set; }
            public string? PhotoPath { get; set; }
        }

        private async Task<List<EmployeeRow>> LoadWorkingEmployees(int? idEmployee = null)
        {
            var query =
                from e in _dbContext.Employees.AsNoTracking()
                join d in _dbContext.Departments.AsNoTracking() on e.IdDepartment equals (int?)d.IdDepartment into dj
                from d in dj.DefaultIfEmpty()
                join g in _dbContext.Designations.AsNoTracking() on e.IdDesignation equals (int?)g.IdDesignation into gj
                from g in gj.DefaultIfEmpty()
                where e.CurrentStatus == WorkingStatus && (e.DateOfBirth != null || e.JoiningDate != null)
                select new
                {
                    e.IdEmployee,
                    e.EmployeeCode,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName,
                    e.EmailID,
                    e.DateOfBirth,
                    e.JoiningDate,
                    DepartmentName = d != null ? d.DepartmentName : null,
                    DesignationName = g != null ? g.DesignationName : null,
                    e.EmployeePhotoFilePath
                };
            if (idEmployee.HasValue)
                query = query.Where(x => x.IdEmployee == idEmployee.Value);

            var rows = await query.ToListAsync();
            return rows.Select(r => new EmployeeRow
            {
                IdEmployee = r.IdEmployee ?? 0,
                EmployeeCode = r.EmployeeCode,
                EmployeeName = string.Join(" ", new[] { r.FirstName, r.MiddleName, r.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                FirstName = r.FirstName,
                EmailID = r.EmailID,
                DateOfBirth = r.DateOfBirth,
                JoiningDate = r.JoiningDate,
                DepartmentName = r.DepartmentName,
                DesignationName = r.DesignationName,
                PhotoPath = r.EmployeePhotoFilePath
            }).ToList();
        }

        /// <summary>
        /// Next date (today or later) with the same day and month. Handles year change (Dec → Jan).
        /// 29-Feb is celebrated on 28-Feb in non-leap years.
        /// </summary>
        public static DateTime NextOccurrence(DateTime date, DateTime today)
        {
            DateTime InYear(int year) =>
                new DateTime(year, date.Month, Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month)));

            var occurrence = InYear(today.Year);
            return occurrence < today.Date ? InYear(today.Year + 1) : occurrence;
        }

        private static CelebrationEmployeeDto ToDto(EmployeeRow e, DateTime occasion, int days) => new()
        {
            IdEmployee = e.IdEmployee,
            EmployeeCode = e.EmployeeCode,
            EmployeeName = e.EmployeeName,
            DepartmentName = e.DepartmentName,
            DesignationName = e.DesignationName,
            OccasionDate = occasion,
            DaysAway = days,
            HasEmail = !string.IsNullOrWhiteSpace(e.EmailID)
        };

        private static async Task LoadPhotos(List<CelebrationEmployeeDto> shown, List<EmployeeRow> employees)
        {
            var pathById = employees.ToDictionary(e => e.IdEmployee, e => e.PhotoPath?.Trim());
            var cache = new Dictionary<int, byte[]?>();
            foreach (var dto in shown)
            {
                if (!cache.TryGetValue(dto.IdEmployee, out var photo))
                {
                    photo = null;
                    if (pathById.TryGetValue(dto.IdEmployee, out var path) && !string.IsNullOrEmpty(path) && File.Exists(path))
                        photo = await File.ReadAllBytesAsync(path);
                    cache[dto.IdEmployee] = photo;
                }
                dto.EmployeePhoto = photo;
            }
        }

        private async Task LoadEmailStatus(List<CelebrationEmployeeDto> list, string entityCode)
        {
            if (list.Count == 0) return;
            var ids = list.Select(x => (int?)x.IdEmployee).ToList();
            var yearStart = new DateTime(DateTime.Today.Year, 1, 1);
            var sent = await _dbContext.Notifications.AsNoTracking()
                .Where(n => n.RelatedRecordType == entityCode && n.CreatedAt >= yearStart && ids.Contains(n.ReceivedByIdEmployee))
                .Select(n => new { n.ReceivedByIdEmployee, n.CreatedAt })
                .ToListAsync();
            var lastSent = sent.GroupBy(s => s.ReceivedByIdEmployee).ToDictionary(g => g.Key ?? 0, g => g.Max(x => x.CreatedAt));

            // Only this year's occasion counts (an upcoming birthday in January is next year's)
            foreach (var dto in list)
                if (dto.OccasionDate.Year == DateTime.Today.Year && lastSent.TryGetValue(dto.IdEmployee, out var at))
                    dto.EmailSentOn = at;
        }

        /// <summary>
        /// Placeholders for the templates. Both the existing #NAME# style and {{Name}} style are supported.
        /// </summary>
        private async Task<Dictionary<string, string>> BuildReplacements(EmployeeRow e, int? yearsOfService)
        {
            var companyName = await _dbContext.SystemParameters.AsNoTracking()
                .Where(p => p.ParameterName == "CompanyName")
                .Select(p => p.ParameterValue)
                .FirstOrDefaultAsync() ?? string.Empty;

            var values = new Dictionary<string, string>
            {
                ["EmployeeName"] = e.EmployeeName,
                ["FirstName"] = e.FirstName ?? e.EmployeeName,
                ["EmployeeCode"] = e.EmployeeCode ?? string.Empty,
                ["CompanyName"] = companyName,
                ["Department"] = e.DepartmentName ?? string.Empty,
                ["Designation"] = e.DesignationName ?? string.Empty,
                ["YearsOfService"] = yearsOfService?.ToString() ?? string.Empty,
                ["JoiningDate"] = e.JoiningDate?.ToString("dd-MMM-yyyy") ?? string.Empty,
                ["CurrentDate"] = DateTime.Today.ToString("dd-MMM-yyyy"),
            };

            var replacements = new Dictionary<string, string>();
            foreach (var kv in values)
            {
                replacements[$"{{{{{kv.Key}}}}}"] = kv.Value;                    // {{EmployeeName}}
                replacements[$"#{kv.Key.ToUpperInvariant()}#"] = kv.Value;       // #EMPLOYEENAME#
            }
            return replacements;
        }

        private static string? Shorten(string? text, int maxLength) =>
            string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength);

        private static string ApplyReplacements(string? text, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            foreach (var kv in replacements)
                text = text.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase);
            return text;
        }
    }
}
