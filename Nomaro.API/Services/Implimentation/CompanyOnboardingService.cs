using System.Data;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implementation
{
    /// <summary>
    /// Company self-onboarding (NOMARO Company Setup Requirements).
    ///   1. Email OTP (6 digits, 5 minutes) -> short-lived registration token.
    ///   2. Register: NOMARO_{CODE} is created on the country's SQL Server by BACKUP / RESTORE of
    ///      NomaroCountries.SourceDatabaseName, SystemParameters are set, an ADMIN employee with every
    ///      permission is created for the registered email, and only then the NomaroInstanceConfig row
    ///      is inserted (so a half-built company is never active).
    ///   3. Every step is recorded in CompanyRegistrations; a failed registration resumes from the
    ///      failed step when the same email submits the same company code again.
    /// </summary>
    public class CompanyOnboardingService : ICompanyOnboardingService
    {
        private const int OtpMinutes = 5;
        private const int TokenMinutes = 30;
        private const int MaxOtpPerEmailPerHour = 5;
        private const int MaxOtpPerIpPerHour = 20;
        private const int MinSecondsBetweenOtps = 30;
        private const int MaxOtpAttempts = 5;
        private const int MaxLogoBytes = 1024 * 1024;
        private const int SqlTimeoutSeconds = 900;

        // statuses / steps
        private const string SPending = "PENDING", SDbCreated = "DB_CREATED", SDbConfigured = "DB_CONFIGURED",
            SAdminCreated = "ADMIN_CREATED", SActive = "ACTIVE", SCompleted = "COMPLETED", SFailed = "FAILED";
        private static readonly string[] StepOrder = { SPending, SDbCreated, SDbConfigured, SAdminCreated, SActive, SCompleted };

        private static readonly Regex CodePattern = new("^[A-Z]{2,10}$", RegexOptions.Compiled);
        private static readonly Regex DbNamePattern = new("^[A-Za-z0-9_]{1,128}$", RegexOptions.Compiled);

        // Words that must never become a company code: TenantMiddleware treats /api/v1/{segment}/ as a company
        // when the segment matches a company code, so a code equal to a controller name would capture its routes.
        private static readonly Lazy<HashSet<string>> ReservedCodes = new(() =>
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "API", "APP", "ADMIN", "LOGIN", "LOGOUT", "REGISTER", "SIGNUP", "AUTH", "DASHBOARD", "REPORTS", "AGREEMENT",
                "SWAGGER", "HEALTH", "STATIC", "ASSETS", "PUBLIC", "NOMARO", "GLOBAL", "TEMPLATE",
                "MASTER", "MODEL", "MSDB", "TEMPDB", "SYSTEM"
            };
            foreach (var t in typeof(CompanyOnboardingService).Assembly.GetTypes())
                if (!t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t) && t.Name.EndsWith("Controller"))
                    set.Add(t.Name[..^"Controller".Length]);
            return set;
        });

        // one registration at a time per API instance (database creation is heavy and codes must stay unique)
        private static readonly SemaphoreSlim RegisterLock = new(1, 1);

        private readonly GlobalDbContext _global;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CompanyOnboardingService> _logger;

        public CompanyOnboardingService(GlobalDbContext global, IConfiguration configuration, IMemoryCache cache, ILogger<CompanyOnboardingService> logger)
        {
            _global = global;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        private sealed class CountryRow
        {
            public string CountryCode { get; set; } = "";
            public string CountryName { get; set; } = "";
            public string? SourceDatabaseName { get; set; }
            public string? SourceServerConnection { get; set; }
            public string? DateFormat { get; set; }
            public string? CurrencySymbol { get; set; }
            public string? CurrencyName { get; set; }
            public string? CurrencyCode { get; set; }
            public string? BaseURL { get; set; }
            public string? TimeZoneId { get; set; }
        }

        private string GlobalConnectionString => _configuration.GetConnectionString("GlobalDbContext")
            ?? throw new InvalidOperationException("The NOMARO_GLOBAL connection string is not configured.");

        private SqlConnection GlobalConnection() => new(GlobalConnectionString);

        #region Countries and codes

        private async Task<List<CountryRow>> LoadCountries()
        {
            await using var cn = GlobalConnection();
            return (await cn.QueryAsync<CountryRow>(
                @"SELECT CountryCode, CountryName, SourceDatabaseName, SourceServerConnection, DateFormat, CurrencySymbol,
                         CurrencyName, CurrencyCode, BaseURL, TimeZoneId
                  FROM dbo.NomaroCountries ORDER BY CountryName")).ToList();
        }

        private static bool Configured(CountryRow c) =>
            !string.IsNullOrWhiteSpace(c.SourceDatabaseName) && !string.IsNullOrWhiteSpace(c.BaseURL);

        public async Task<List<OnboardingCountryDto>> GetCountries() =>
            (await LoadCountries()).Where(Configured)
                .Select(c => new OnboardingCountryDto { CountryCode = c.CountryCode.Trim(), CountryName = c.CountryName.Trim() })
                .ToList();

        private async Task<CountryRow> GetCountry(string? countryCode)
        {
            var code = (countryCode ?? "").Trim().ToUpperInvariant();
            var country = (await LoadCountries()).FirstOrDefault(c => c.CountryCode.Trim().Equals(code, StringComparison.OrdinalIgnoreCase));
            if (country == null || !Configured(country)) throw new InvalidOperationException("Select a country.");
            if (!DbNamePattern.IsMatch(country.SourceDatabaseName!.Trim()))
                throw new InvalidOperationException("The template database for this country is not configured correctly. Contact support.");
            return country;
        }

        private static string NormaliseCode(string? code) => (code ?? "").Trim().ToUpperInvariant();
        private static string DatabaseNameFor(string code) => $"NOMARO_{code}";

        private static string ServerConnection(CountryRow country, string fallback, string database)
        {
            var b = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(country.SourceServerConnection) ? fallback : country.SourceServerConnection)
            {
                InitialCatalog = database
            };
            return b.ConnectionString;
        }

        /// <summary>null = available, else the reason.</summary>
        private async Task<string?> CodeProblem(string code, CountryRow? country, string? email)
        {
            if (code.Length == 0) return "Enter the company code.";
            if (!CodePattern.IsMatch(code)) return "Use 2 to 10 letters (A-Z) only.";
            if (ReservedCodes.Value.Contains(code)) return $"\"{code}\" is reserved. Choose another code.";

            if (await _global.NomaroInstanceConfigs.AsNoTracking().AnyAsync(c => c.CompanyCode != null && c.CompanyCode.ToUpper() == code))
                return $"\"{code}\" is already registered.";

            var since = DateTime.UtcNow.AddHours(-24);
            var other = await _global.Set<CompanyRegistration>().AsNoTracking()
                .Where(r => r.CompanyCode == code && r.Status != SCompleted && (r.UpdatedOn ?? r.CreatedOn) >= since)
                .Select(r => r.Email).ToListAsync();
            if (other.Any(e => !e.Equals(email ?? "", StringComparison.OrdinalIgnoreCase)))
                return $"\"{code}\" is being registered by someone else. Choose another code.";

            if (country != null)
            {
                var ours = email != null && await _global.Set<CompanyRegistration>().AsNoTracking()
                    .AnyAsync(r => r.CompanyCode == code && r.Email == email && r.Status != SCompleted);
                if (!ours && await DatabaseExists(country, DatabaseNameFor(code)))
                    return $"\"{code}\" cannot be used. Choose another code.";
            }
            return null;
        }

        public async Task<OnboardingCodeCheckDto> CheckCompanyCode(string? companyCode, string? countryCode, string? email)
        {
            var code = NormaliseCode(companyCode);
            CountryRow? country = null;
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                try { country = await GetCountry(countryCode); } catch (InvalidOperationException) { country = null; }
            }
            string? problem;
            try { problem = await CodeProblem(code, country, NormaliseEmail(email, false)); }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Company code check failed for {Code}", code);
                problem = null; // the server is checked again at registration
            }
            return new OnboardingCodeCheckDto { CompanyCode = code, Available = problem == null, Message = problem ?? $"\"{code}\" is available." };
        }

        #endregion

        #region OTP

        private static string? NormaliseEmail(string? email, bool required = true)
        {
            var e = (email ?? "").Trim().ToLowerInvariant();
            if (e.Length == 0) return required ? throw new InvalidOperationException("Enter the email address.") : null;
            if (e.Length > 200) throw new InvalidOperationException("Email address is too long.");
            try
            {
                var parsed = new MailAddress(e);
                if (parsed.Address != e || !e.Contains('.', StringComparison.Ordinal)) throw new FormatException();
            }
            catch (FormatException) { throw new InvalidOperationException("Enter a valid email address."); }
            return e;
        }

        private static string Sha(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            return at <= 1 ? "***" + email[Math.Max(at, 0)..] : email[..1] + new string('*', Math.Min(at - 1, 6)) + email[at..];
        }

        public async Task<OnboardingOtpResultDto> SendOtp(string? email, string? ipAddress)
        {
            var e = NormaliseEmail(email)!;
            var now = DateTime.UtcNow;
            var hourAgo = now.AddHours(-1);
            var otps = _global.Set<CompanyRegistrationOtp>();

            var recent = await otps.AsNoTracking().Where(o => o.Email == e && o.CreatedOn >= hourAgo).OrderByDescending(o => o.CreatedOn).ToListAsync();
            if (recent.Count >= MaxOtpPerEmailPerHour) throw new InvalidOperationException("Too many codes requested for this email. Try again in an hour.");
            if (recent.Count > 0 && (now - recent[0].CreatedOn).TotalSeconds < MinSecondsBetweenOtps)
                throw new InvalidOperationException($"Please wait {MinSecondsBetweenOtps} seconds before requesting another code.");
            if (!string.IsNullOrWhiteSpace(ipAddress) &&
                await otps.CountAsync(o => o.IPAddress == ipAddress && o.CreatedOn >= hourAgo) >= MaxOtpPerIpPerHour)
                throw new InvalidOperationException("Too many codes requested. Try again later.");

            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            otps.Add(new CompanyRegistrationOtp
            {
                Email = e,
                OtpHash = Sha($"{e}:{otp}"),
                ExpiresOn = now.AddMinutes(OtpMinutes),
                IPAddress = Trim(ipAddress, 50),
                CreatedOn = now
            });
            await _global.SaveChangesAsync();

            var (subject, body) = await Template("COMPANY_ONBOARDING_OTP", new Dictionary<string, string>
            {
                ["OTP"] = otp,
                ["OTPMINUTES"] = OtpMinutes.ToString()
            },
            "Your NOMARO verification code: #OTP#",
            "<p>Your one-time code to verify this email address for the NOMARO company registration is:</p>" +
            "<p style=\"font-size:26px;font-weight:bold;letter-spacing:5px\">#OTP#</p><p>The code is valid for #OTPMINUTES# minutes.</p>");

            try { await SendMail(e, subject, body); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onboarding OTP email to {Email} failed", MaskEmail(e));
                throw new InvalidOperationException("The verification email could not be sent. Check the address or try again later.");
            }
            return new OnboardingOtpResultDto { ValidSeconds = OtpMinutes * 60, MaskedEmail = MaskEmail(e) };
        }

        public async Task<OnboardingVerifiedDto> VerifyOtp(string? email, string? otp)
        {
            var e = NormaliseEmail(email)!;
            var code = (otp ?? "").Trim();
            if (!Regex.IsMatch(code, "^[0-9]{6}$")) throw new InvalidOperationException("Enter the 6-digit code.");

            var row = await _global.Set<CompanyRegistrationOtp>()
                .Where(o => o.Email == e && o.VerifiedOn == null)
                .OrderByDescending(o => o.CreatedOn).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Request a new code.");
            if (row.ExpiresOn < DateTime.UtcNow) throw new InvalidOperationException("The code has expired. Request a new code.");
            if (row.FailedAttempts >= MaxOtpAttempts) throw new InvalidOperationException("Too many wrong attempts. Request a new code.");

            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(row.OtpHash), Encoding.ASCII.GetBytes(Sha($"{e}:{code}"))))
            {
                row.FailedAttempts++;
                await _global.SaveChangesAsync();
                var left = MaxOtpAttempts - row.FailedAttempts;
                throw new InvalidOperationException(left > 0 ? $"The code is not correct. {left} attempt(s) left." : "Too many wrong attempts. Request a new code.");
            }

            var token = WebEncoders(RandomNumberGenerator.GetBytes(32));
            row.VerifiedOn = DateTime.UtcNow;
            row.TokenHash = Sha(token);
            row.TokenExpiresOn = DateTime.UtcNow.AddMinutes(TokenMinutes);
            await _global.SaveChangesAsync();
            return new OnboardingVerifiedDto { Token = token, ValidMinutes = TokenMinutes };
        }

        private static string WebEncoders(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        #endregion

        #region Register

        public async Task<OnboardingResultDto> Register(OnboardingRegisterDto dto, string? ipAddress)
        {
            if (dto == null) throw new InvalidOperationException("Enter the company details.");
            var email = NormaliseEmail(dto.Email)!;
            var code = NormaliseCode(dto.CompanyCode);
            var name = (dto.CompanyName ?? "").Trim();
            var address = (dto.CompanyAddress ?? "").Trim();
            if (name.Length == 0) throw new InvalidOperationException("Enter the company name.");
            if (name.Length > 200) throw new InvalidOperationException("Company name must not exceed 200 characters.");
            if (address.Length == 0) throw new InvalidOperationException("Enter the company address.");
            if (address.Length > 500) throw new InvalidOperationException("Company address must not exceed 500 characters.");
            var contactName = Regex.Replace((dto.ContactName ?? "").Trim(), @"\s+", " ");
            if (contactName.Length == 0) throw new InvalidOperationException("Enter your name.");
            if (contactName.Length > 100) throw new InvalidOperationException("Name must not exceed 100 characters.");
            var mobile = Regex.Replace((dto.ContactMobile ?? "").Trim(), @"[\s\-()]", "");
            if (mobile.Length == 0) throw new InvalidOperationException("Enter your mobile number.");
            if (!Regex.IsMatch(mobile, @"^\+?[0-9]{7,15}$")) throw new InvalidOperationException("Enter a valid mobile number (7 to 15 digits, + allowed at the start).");
            var (logo, logoType) = await ReadLogo(dto.Logo);

            var otpRow = await VerifiedToken(email, dto.Token);
            var country = await GetCountry(dto.CountryCode);

            if (!await RegisterLock.WaitAsync(TimeSpan.FromMinutes(20)))
                throw new InvalidOperationException("The server is busy creating another company. Try again in a few minutes.");
            try
            {
                var regs = _global.Set<CompanyRegistration>();
                var reg = await regs.Where(r => r.CompanyCode == code && r.Email == email && r.Status != SCompleted)
                    .OrderByDescending(r => r.IdCompanyRegistration).FirstOrDefaultAsync();

                if (reg != null && !reg.CountryCode.Equals(country.CountryCode.Trim(), StringComparison.OrdinalIgnoreCase)
                    && Reached(reg, SDbCreated))
                    throw new InvalidOperationException("This company code was started for another country. Contact support to change it.");

                var problem = await CodeProblem(code, country, email);
                if (problem != null) throw new InvalidOperationException(problem);

                if (reg == null)
                {
                    reg = new CompanyRegistration
                    {
                        CountryCode = country.CountryCode.Trim(),
                        CompanyCode = code,
                        Email = email,
                        Status = SPending,
                        CreatedOn = DateTime.UtcNow
                    };
                    regs.Add(reg);
                }
                // details may be corrected on a retry
                reg.CountryCode = country.CountryCode.Trim();
                reg.CompanyName = name;
                reg.CompanyAddress = address;
                reg.ContactName = contactName;
                reg.ContactMobile = mobile;
                if (logo != null) { reg.LogoContent = logo; reg.LogoMimeType = logoType; }
                reg.DatabaseName = DatabaseNameFor(code);
                reg.CompanyUrl = $"{country.BaseURL!.Trim().TrimEnd('/')}/{code}";
                reg.IPAddress = Trim(ipAddress, 50);
                reg.Attempts++;
                reg.ErrorMessage = null;
                reg.UpdatedOn = DateTime.UtcNow;
                await _global.SaveChangesAsync();

                var result = await RunSteps(reg, country);

                otpRow.UsedOn = DateTime.UtcNow;
                await _global.SaveChangesAsync();
                return result;
            }
            finally
            {
                RegisterLock.Release();
            }
        }

        private async Task<CompanyRegistrationOtp> VerifiedToken(string email, string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Verify your email address first.");
            var hash = Sha(token.Trim());
            var row = await _global.Set<CompanyRegistrationOtp>()
                .FirstOrDefaultAsync(o => o.Email == email && o.TokenHash == hash);
            if (row == null || row.TokenExpiresOn == null || row.TokenExpiresOn < DateTime.UtcNow)
                throw new InvalidOperationException("Your email verification has expired. Verify your email again.");
            return row;
        }

        private static async Task<(byte[]? bytes, string? type)> ReadLogo(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (null, null);
            if (file.Length > MaxLogoBytes) throw new InvalidOperationException("The logo must be 1 MB or smaller.");
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var b = ms.ToArray();
            // check the actual content, not the file name
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return (b, "image/png");
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return (b, "image/jpeg");
            throw new InvalidOperationException("The logo must be a PNG or JPG image.");
        }

        private static bool Reached(CompanyRegistration r, string step)
        {
            var last = r.LastStep ?? SPending;
            return Array.IndexOf(StepOrder, last) >= Array.IndexOf(StepOrder, step);
        }

        private async Task<OnboardingResultDto> RunSteps(CompanyRegistration reg, CountryRow country)
        {
            var fallback = GlobalConnectionString;
            var dbName = reg.DatabaseName!;
            var tenantConnection = ServerConnection(country, fallback, dbName);
            string step = SDbCreated;
            try
            {
                if (!Reached(reg, SDbCreated))
                {
                    step = SDbCreated;
                    await CreateDatabase(country, fallback, country.SourceDatabaseName!.Trim(), dbName);
                    await Done(reg, SDbCreated);
                }
                if (!Reached(reg, SDbConfigured))
                {
                    step = SDbConfigured;
                    await ConfigureDatabase(tenantConnection, reg);
                    await Done(reg, SDbConfigured);
                }
                if (!Reached(reg, SAdminCreated))
                {
                    step = SAdminCreated;
                    await CreateAdmin(tenantConnection, reg);
                    await Done(reg, SAdminCreated);
                }
                if (!Reached(reg, SActive))
                {
                    step = SActive;
                    await Activate(reg, country, tenantConnection);
                    await Done(reg, SActive);
                }
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Company registration {Code} failed at {Step}", reg.CompanyCode, step);
                _global.ChangeTracker.Clear();
                var fresh = await _global.Set<CompanyRegistration>().FirstAsync(r => r.IdCompanyRegistration == reg.IdCompanyRegistration);
                fresh.Status = SFailed;
                fresh.ErrorMessage = $"{step}: {root.Message}";
                fresh.UpdatedOn = DateTime.UtcNow;
                await _global.SaveChangesAsync();
                var support = await GlobalParam("OnboardingSupportEmail");
                throw new InvalidOperationException(
                    $"The company could not be set up ({StepText(step)}). Nothing is active yet; submit again to retry" +
                    (string.IsNullOrWhiteSpace(support) ? "." : $", or contact {support}."));
            }

            // the company is live; the email is best effort
            var sent = false;
            try
            {
                var countryName = country.CountryName.Trim();
                var (subject, body) = await Template("COMPANY_ONBOARDING", new Dictionary<string, string>
                {
                    ["COMPANYNAME"] = WebUtility.HtmlEncode(reg.CompanyName),
                    ["COMPANYCODE"] = reg.CompanyCode,
                    ["COUNTRYNAME"] = WebUtility.HtmlEncode(countryName),
                    ["COMPANYURL"] = reg.CompanyUrl!,
                    ["ADMINEMAIL"] = reg.Email,
                    ["ADMINNAME"] = WebUtility.HtmlEncode(reg.ContactName ?? "Administrator")
                },
                "Welcome to NOMARO, #COMPANYNAME#",
                "<p><b>#COMPANYNAME#</b> (#COMPANYCODE#) is now registered with NOMARO.</p><p>Your company site: <a href=\"#COMPANYURL#\">#COMPANYURL#</a></p>" +
                "<p>Log in with #ADMINEMAIL# using \"Login with OTP\".</p>");
                await SendMail(reg.Email, subject, body);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration confirmation email for {Code} failed", reg.CompanyCode);
            }

            reg.Status = SCompleted;
            reg.LastStep = SCompleted;
            reg.CompletedOn = DateTime.UtcNow;
            reg.UpdatedOn = DateTime.UtcNow;
            reg.ErrorMessage = sent ? null : "Completed, but the confirmation email could not be sent.";
            await _global.SaveChangesAsync();

            return new OnboardingResultDto
            {
                CompanyCode = reg.CompanyCode,
                CompanyName = reg.CompanyName,
                CompanyUrl = reg.CompanyUrl!,
                AdminEmail = reg.Email,
                EmailSent = sent
            };
        }

        private static string StepText(string step) => step switch
        {
            SDbCreated => "creating the database",
            SDbConfigured => "saving the company settings",
            SAdminCreated => "creating the administrator",
            SActive => "activating the company",
            _ => step
        };

        private async Task Done(CompanyRegistration reg, string step)
        {
            reg.LastStep = step;
            reg.Status = step;
            reg.UpdatedOn = DateTime.UtcNow;
            await _global.SaveChangesAsync();
        }

        #endregion

        #region Database steps

        private async Task<bool> DatabaseExists(CountryRow country, string dbName)
        {
            await using var cn = new SqlConnection(ServerConnection(country, GlobalConnectionString, "master"));
            return await cn.ExecuteScalarAsync<int?>("SELECT database_id FROM sys.databases WHERE name = @dbName", new { dbName }) != null;
        }

        private sealed class BackupFile
        {
            public string LogicalName { get; set; } = "";
            public string PhysicalName { get; set; } = "";
            public string Type { get; set; } = "";
        }

        /// <summary>BACKUP (copy-only) the template and RESTORE it as the new database on the same server.</summary>
        private async Task CreateDatabase(CountryRow country, string fallback, string source, string target)
        {
            if (!DbNamePattern.IsMatch(source) || !DbNamePattern.IsMatch(target)) throw new InvalidOperationException("Invalid database name.");
            await using var cn = new SqlConnection(ServerConnection(country, fallback, "master"));
            await cn.OpenAsync();

            if (await cn.ExecuteScalarAsync<int?>("SELECT database_id FROM sys.databases WHERE name = @source", new { source }) == null)
                throw new InvalidOperationException($"Template database {source} was not found on the country's server.");

            // A database left by an earlier failed attempt of THIS registration is removed first.
            // (A database that existed before registration is rejected earlier by CodeProblem.)
            if (await cn.ExecuteScalarAsync<int?>("SELECT database_id FROM sys.databases WHERE name = @target", new { target }) != null)
            {
                if (await _global.NomaroInstanceConfigs.AsNoTracking().AnyAsync(c => c.DBConnectionString != null && c.CompanyCode != null &&
                        c.CompanyCode.ToUpper() == target.Substring("NOMARO_".Length)))
                    throw new InvalidOperationException("The company database already belongs to an active company.");
                await cn.ExecuteAsync($"ALTER DATABASE {Q(target)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Q(target)};",
                    commandTimeout: SqlTimeoutSeconds);
            }

            var paths = await cn.QueryFirstAsync<(string? data, string? log, string? backup)>(
                @"SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(512)),
                         CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(512)),
                         CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512))");
            var sourceDataFile = await cn.ExecuteScalarAsync<string?>(
                "SELECT TOP 1 physical_name FROM sys.master_files WHERE database_id = DB_ID(@source) AND type = 0 ORDER BY file_id", new { source });
            var sourceDir = DirectoryOf(sourceDataFile);
            var dataDir = WithSeparator(string.IsNullOrWhiteSpace(paths.data) ? sourceDir : paths.data!);
            var logDir = WithSeparator(string.IsNullOrWhiteSpace(paths.log) ? dataDir : paths.log!);
            var backupDir = WithSeparator(string.IsNullOrWhiteSpace(paths.backup) ? dataDir : paths.backup!);
            if (string.IsNullOrWhiteSpace(dataDir)) throw new InvalidOperationException("The SQL Server data folder could not be found.");

            var backupFile = $"{backupDir}{target}_onboarding.bak";
            await cn.ExecuteAsync("BACKUP DATABASE @source TO DISK = @backupFile WITH COPY_ONLY, INIT, FORMAT",
                new { source, backupFile }, commandTimeout: SqlTimeoutSeconds);

            try
            {
                var files = (await cn.QueryAsync<BackupFile>("RESTORE FILELISTONLY FROM DISK = @backupFile", new { backupFile },
                    commandTimeout: SqlTimeoutSeconds)).ToList();
                if (files.Count == 0) throw new InvalidOperationException("The template backup contains no files.");

                var moves = new List<string>();
                var p = new DynamicParameters();
                p.Add("target", target);
                p.Add("backupFile", backupFile);
                int d = 0, l = 0, s = 0;
                for (var i = 0; i < files.Count; i++)
                {
                    var f = files[i];
                    string physical = f.Type.Trim().ToUpperInvariant() switch
                    {
                        "L" => $"{logDir}{target}_log{(l++ == 0 ? "" : l.ToString())}.ldf",
                        "S" => $"{dataDir}{target}_fs{++s}",
                        _ => $"{dataDir}{target}{(d++ == 0 ? ".mdf" : $"_{d}.ndf")}"
                    };
                    p.Add($"l{i}", f.LogicalName);
                    p.Add($"f{i}", physical);
                    moves.Add($"MOVE @l{i} TO @f{i}");
                }
                await cn.ExecuteAsync($"RESTORE DATABASE @target FROM DISK = @backupFile WITH {string.Join(", ", moves)}, RECOVERY", p,
                    commandTimeout: SqlTimeoutSeconds);
            }
            finally
            {
                try { await cn.ExecuteAsync("EXEC master.sys.xp_delete_file 0, @backupFile", new { backupFile }); }
                catch (Exception ex) { _logger.LogWarning(ex, "Onboarding backup file {File} could not be removed", backupFile); }
            }

            var state = await cn.ExecuteScalarAsync<string?>("SELECT state_desc FROM sys.databases WHERE name = @target", new { target });
            if (!string.Equals(state, "ONLINE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Database {target} is {state ?? "missing"} after the restore.");
        }

        private static string Q(string name) => "[" + name.Replace("]", "]]") + "]";

        private static string? DirectoryOf(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var i = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            return i < 0 ? null : path[..(i + 1)];
        }

        private static string WithSeparator(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return "";
            var sep = dir.Contains('/') && !dir.Contains('\\') ? '/' : '\\';
            return dir.EndsWith(sep) ? dir : dir + sep;
        }

        // @setValue / @setBinary choose which column(s) are written; the other keeps its current value
        private const string UpsertParameterSql = @"
IF EXISTS (SELECT 1 FROM dbo.SystemParameters WHERE ParameterName = @name)
    UPDATE dbo.SystemParameters
    SET ParameterValue = CASE WHEN @setValue = 1 THEN @value ELSE ParameterValue END,
        ParameterBinaryValue = CASE WHEN @setBinary = 1 THEN @binary ELSE ParameterBinaryValue END
    WHERE ParameterName = @name;
ELSE IF COLUMNPROPERTY(OBJECT_ID('dbo.SystemParameters'), 'IdSystemParameter', 'IsIdentity') = 1
    INSERT INTO dbo.SystemParameters (ParameterName, ParameterDescription, ParameterValue, ParameterBinaryValue, DataType, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
    VALUES (@name, @description, CASE WHEN @setValue = 1 THEN @value END, CASE WHEN @setBinary = 1 THEN @binary END, @dataType, @show, @setValue, @setBinary);
ELSE
    INSERT INTO dbo.SystemParameters (IdSystemParameter, ParameterName, ParameterDescription, ParameterValue, ParameterBinaryValue, DataType, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
    SELECT ISNULL(MAX(IdSystemParameter), 0) + 1, @name, @description, CASE WHEN @setValue = 1 THEN @value END, CASE WHEN @setBinary = 1 THEN @binary END, @dataType, @show, @setValue, @setBinary
    FROM dbo.SystemParameters;";

        /// <summary>
        /// Company settings in the new database (requirement section 5):
        ///   CompanyName     ParameterValue = company name, ParameterBinaryValue = logo
        ///   CompanyLogo     ParameterBinaryValue = logo
        ///   ProductLogo     ParameterBinaryValue = logo
        ///   CompanyAddress  ParameterValue = address
        ///   BrowserConfig   ParameterValue = company code
        /// Missing rows are inserted. EmailSettings is copied from NOMARO_GLOBAL when the template has none.
        /// Without a logo the template's logo values are left as they are.
        /// </summary>
        private async Task ConfigureDatabase(string connection, CompanyRegistration reg)
        {
            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            async Task Set(string name, string description, string dataType, bool setValue, string? value, bool setBinary, byte[]? binary, bool show = true)
            {
                var p = new DynamicParameters();
                p.Add("name", name, DbType.String);
                p.Add("description", description, DbType.String);
                p.Add("value", value, DbType.String);
                p.Add("binary", binary, DbType.Binary, size: -1);   // typed, so a NULL is varbinary, not nvarchar
                p.Add("setValue", setValue, DbType.Boolean);
                p.Add("setBinary", setBinary, DbType.Boolean);
                p.Add("dataType", dataType, DbType.String);
                p.Add("show", show, DbType.Boolean);
                await cn.ExecuteAsync(UpsertParameterSql, p, tx);
            }

            var logo = reg.LogoContent;
            var hasLogo = logo != null;
            await Set("CompanyName", "Company name", "STRING", true, reg.CompanyName, hasLogo, logo);
            await Set("CompanyAddress", "Company address", "STRING", true, reg.CompanyAddress, false, null);
            await Set("BrowserConfig", "Company code used in the browser address", "STRING", true, reg.CompanyCode, false, null);
            if (hasLogo)
            {
                await Set("CompanyLogo", "Company logo", "IMAGE", false, null, true, logo);
                await Set("ProductLogo", "Logo shown in the application", "IMAGE", false, null, true, logo);
            }

            var hasEmail = await cn.ExecuteScalarAsync<string?>(
                "SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'EmailSettings'", transaction: tx);
            if (string.IsNullOrWhiteSpace(hasEmail))
            {
                var globalEmail = await GlobalParam("EmailSettings");
                if (!string.IsNullOrWhiteSpace(globalEmail))
                    await Set("EmailSettings", "SMTP account used for this company's emails", "JSON", true, globalEmail, false, null, show: false);
            }
            await tx.CommitAsync();
        }

        /// <summary>First word is the first name, the rest is the last name; falls back to System Administrator.</summary>
        private static (string First, string Last) SplitName(string? name)
        {
            var n = Regex.Replace((name ?? "").Trim(), @"\s+", " ");
            if (n.Length == 0) return ("System", "Administrator");
            var i = n.IndexOf(' ');
            return i < 0 ? (n, "") : (n[..i], n[(i + 1)..]);
        }

        /// <summary>The ADMIN employee: the registered email, active, every Payroll screen with all its permissions.</summary>
        private async Task CreateAdmin(string connection, CompanyRegistration reg)
        {
            const string sql = @"
DECLARE @id INT = (SELECT TOP 1 IdEmployee FROM dbo.Employees WHERE EmployeeCode = 'ADMIN' ORDER BY IdEmployee);
DECLARE @code NVARCHAR(50), @n INT;

-- Department 'IT Department': use it, or create it
DECLARE @dept INT = (SELECT TOP 1 IdDepartment FROM dbo.Departments WHERE LTRIM(RTRIM(DepartmentName)) = 'IT Department' ORDER BY IdDepartment);
IF @dept IS NULL
BEGIN
    SET @code = 'IT'; SET @n = 1;
    WHILE EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentCode = @code) BEGIN SET @code = 'IT' + CAST(@n AS NVARCHAR(10)); SET @n += 1; END
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Departments'), 'IdDepartment', 'IsIdentity') = 1
    BEGIN
        INSERT INTO dbo.Departments (DepartmentCode, DepartmentName) VALUES (@code, 'IT Department');
        SET @dept = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        SELECT @dept = ISNULL(MAX(IdDepartment), 0) + 1 FROM dbo.Departments;
        INSERT INTO dbo.Departments (IdDepartment, DepartmentCode, DepartmentName) VALUES (@dept, @code, 'IT Department');
    END
END

-- Designation 'Systems Administrator': use it, or create it
DECLARE @desig INT = (SELECT TOP 1 IdDesignation FROM dbo.Designations WHERE LTRIM(RTRIM(DesignationName)) = 'Systems Administrator' ORDER BY IdDesignation);
IF @desig IS NULL
BEGIN
    SET @code = 'SYSADMIN'; SET @n = 1;
    WHILE EXISTS (SELECT 1 FROM dbo.Designations WHERE DesignationCode = @code) BEGIN SET @code = 'SYSADMIN' + CAST(@n AS NVARCHAR(10)); SET @n += 1; END
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Designations'), 'IdDesignation', 'IsIdentity') = 1
    BEGIN
        INSERT INTO dbo.Designations (DesignationCode, DesignationName, IsOvertimeAllowanceAllowed) VALUES (@code, 'Systems Administrator', 0);
        SET @desig = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        SELECT @desig = ISNULL(MAX(IdDesignation), 0) + 1 FROM dbo.Designations;
        INSERT INTO dbo.Designations (IdDesignation, DesignationCode, DesignationName, IsOvertimeAllowanceAllowed) VALUES (@desig, @code, 'Systems Administrator', 0);
    END
END

-- the registered email must identify only the administrator
UPDATE dbo.Employees SET EmailID = NULL WHERE EmailID = @email AND (@id IS NULL OR IdEmployee <> @id);

IF @id IS NULL
BEGIN
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Employees'), 'IdEmployee', 'IsIdentity') = 1
    BEGIN
        INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, EmailID, PhoneNumber2, IdDepartment, IdDesignation, JoiningDate, CurrentStatus, EmployeeWorkType, OverTimeAllowedStatus, FailedLoginAttempts)
        VALUES ('ADMIN', @firstName, @lastName, @email, @mobile, @dept, @desig, CAST(GETDATE() AS date), 'Working', 'Permanent', 'false', 0);
        SET @id = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        SELECT @id = ISNULL(MAX(IdEmployee), 0) + 1 FROM dbo.Employees;
        INSERT INTO dbo.Employees (IdEmployee, EmployeeCode, FirstName, LastName, EmailID, PhoneNumber2, IdDepartment, IdDesignation, JoiningDate, CurrentStatus, EmployeeWorkType, OverTimeAllowedStatus, FailedLoginAttempts)
        VALUES (@id, 'ADMIN', @firstName, @lastName, @email, @mobile, @dept, @desig, CAST(GETDATE() AS date), 'Working', 'Permanent', 'false', 0);
    END
END
ELSE
    UPDATE dbo.Employees
    SET EmailID = @email, FirstName = @firstName, LastName = @lastName, PhoneNumber2 = @mobile,
        IdDepartment = @dept, IdDesignation = @desig, CurrentStatus = 'Working', LastWorkingDay = NULL, PasswordHash = NULL, PasswordUpdatedOn = NULL,
        FailedLoginAttempts = 0, LockoutEndTime = NULL
    WHERE IdEmployee = @id;

DELETE FROM dbo.EmployeePermissions WHERE IdEmployee = @id;
IF COLUMNPROPERTY(OBJECT_ID('dbo.EmployeePermissions'), 'IdEmployeePermission', 'IsIdentity') = 1
    INSERT INTO dbo.EmployeePermissions (IdEmployee, IdPayrollScreen, Permission)
    SELECT @id, IdPayrollScreen, ISNULL(NULLIF(LTRIM(RTRIM(ValidPermissions)), ''), 'V')
    FROM dbo.PayrollScreens WHERE APPTYPE = 'PAYROLL';
ELSE
    INSERT INTO dbo.EmployeePermissions (IdEmployeePermission, IdEmployee, IdPayrollScreen, Permission)
    SELECT (SELECT ISNULL(MAX(IdEmployeePermission), 0) FROM dbo.EmployeePermissions) + ROW_NUMBER() OVER (ORDER BY IdPayrollScreen),
           @id, IdPayrollScreen, ISNULL(NULLIF(LTRIM(RTRIM(ValidPermissions)), ''), 'V')
    FROM dbo.PayrollScreens WHERE APPTYPE = 'PAYROLL';

SELECT @id;";
            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var (firstName, lastName) = SplitName(reg.ContactName);
            var id = await cn.ExecuteScalarAsync<int?>(sql, new
            {
                email = reg.Email,
                firstName,
                lastName,
                mobile = string.IsNullOrWhiteSpace(reg.ContactMobile) ? null : reg.ContactMobile
            }, tx, commandTimeout: 120);
            if (id is not > 0) throw new InvalidOperationException("The administrator could not be created.");
            await tx.CommitAsync();
        }

        /// <summary>Last step: the NomaroInstanceConfig row makes the company reachable.</summary>
        private async Task Activate(CompanyRegistration reg, CountryRow country, string connection)
        {
            // the new database must answer before it is published
            await using (var cn = new SqlConnection(connection))
                await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.SystemParameters");

            var exists = await _global.NomaroInstanceConfigs.AnyAsync(c => c.CompanyCode != null && c.CompanyCode.ToUpper() == reg.CompanyCode);
            if (!exists)
            {
                _global.NomaroInstanceConfigs.Add(new NomaroInstanceConfig
                {
                    CountryCode = country.CountryCode.Trim(),
                    CountryName = country.CountryName.Trim(),
                    CompanyCode = reg.CompanyCode,
                    CompanyName = reg.CompanyName,
                    DBConnectionString = connection,
                    DateFormat = country.DateFormat,
                    CurrencySymbol = country.CurrencySymbol,
                    CurrencyName = country.CurrencyName,
                    CurrencyCode = country.CurrencyCode,
                    TimeZoneId = country.TimeZoneId
                });
                await _global.SaveChangesAsync();
            }
            // TenantService caches unknown codes for 5 minutes
            _cache.Remove($"tenant_config_{reg.CompanyCode.ToUpperInvariant()}");
        }

        #endregion

        #region Email

        private async Task<string?> GlobalParam(string name)
        {
            try
            {
                await using var cn = GlobalConnection();
                return await cn.ExecuteScalarAsync<string?>(
                    "SELECT TOP 1 ParameterValue FROM dbo.GlobalSystemParameters WHERE ParameterName = @name", new { name });
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "GlobalSystemParameters {Name} could not be read", name);
                return null;
            }
        }

        private async Task<(string subject, string body)> Template(string entityCode, Dictionary<string, string> values, string defaultSubject, string defaultBody)
        {
            string Apply(string? text) => values.Aggregate(text ?? "", (t, kv) =>
                t.Replace($"#{kv.Key}#", kv.Value, StringComparison.OrdinalIgnoreCase)
                 .Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.OrdinalIgnoreCase));
            try
            {
                await using var cn = GlobalConnection();
                var row = await cn.QueryFirstOrDefaultAsync<(string? subject, string? content)>(
                    "SELECT TOP 1 EmailSubject, EmailContent FROM dbo.GlobalNotificationsConfig WHERE EntityCode = @entityCode", new { entityCode });
                if (!string.IsNullOrWhiteSpace(row.subject) && !string.IsNullOrWhiteSpace(row.content))
                    return (Apply(row.subject), Apply(row.content));
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "GlobalNotificationsConfig {Code} could not be read", entityCode);
            }
            _logger.LogWarning("Email template {Code} not found in GlobalNotificationsConfig; the built-in text was used.", entityCode);
            return (Apply(defaultSubject), Apply(defaultBody));
        }

        private sealed class SmtpSettings
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
            public string? SmtpHost { get; set; }
            public int SmtpPort { get; set; }
            public bool EnableSsl { get; set; }
            public string? DisplayName { get; set; }
        }

        /// <summary>Sends with NOMARO_GLOBAL.GlobalSystemParameters 'EmailSettings' (same JSON as a company's EmailSettings).</summary>
        private async Task SendMail(string to, string subject, string body)
        {
            var json = await GlobalParam("EmailSettings");
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("EmailSettings is missing in NOMARO_GLOBAL.GlobalSystemParameters.");
            var trimmed = json.Trim();
            if (trimmed.StartsWith("\"EmailSettings\"", StringComparison.OrdinalIgnoreCase)) trimmed = "{" + trimmed.TrimEnd().TrimEnd(',') + "}";
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            if (root.TryGetProperty("EmailSettings", out var inner) && inner.ValueKind == JsonValueKind.Object) root = inner;
            var s = JsonSerializer.Deserialize<SmtpSettings>(root.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("EmailSettings could not be read.");
            if (string.IsNullOrWhiteSpace(s.SmtpHost) || string.IsNullOrWhiteSpace(s.Email))
                throw new InvalidOperationException("EmailSettings in NOMARO_GLOBAL is incomplete.");

            using var client = new SmtpClient(s.SmtpHost, s.SmtpPort == 0 ? 587 : s.SmtpPort)
            {
                EnableSsl = s.EnableSsl,
                Credentials = new NetworkCredential(s.Email, s.Password)
            };
            using var message = new MailMessage
            {
                From = new MailAddress(s.Email, string.IsNullOrWhiteSpace(s.DisplayName) ? s.Email : s.DisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);
            await client.SendMailAsync(message);
        }

        #endregion

        private static string? Trim(string? value, int max) => string.IsNullOrEmpty(value) ? value : value.Length > max ? value[..max] : value;
    }
}
