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
            public string? TaxEngine { get; set; }
        }

        private string GlobalConnectionString => _configuration.GetConnectionString("GlobalDbContext")
            ?? throw new InvalidOperationException("The NOMARO_GLOBAL connection string is not configured.");

        private SqlConnection GlobalConnection() => new(GlobalConnectionString);

        #region Countries and codes

        private async Task<List<CountryRow>> LoadCountries()
        {
            await using var cn = GlobalConnection();
            return (await cn.QueryAsync<CountryRow>(
                @"SELECT c.CountryCode, c.CountryName,
                         -- one source database for all countries (GlobalSystemParameters) unless a country names its own
                         COALESCE(NULLIF(LTRIM(RTRIM(c.SourceDatabaseName)), ''),
                                  (SELECT TOP 1 NULLIF(LTRIM(RTRIM(g.ParameterValue)), '') FROM dbo.GlobalSystemParameters g
                                   WHERE g.ParameterName = 'OnboardingSourceDatabase')) AS SourceDatabaseName,
                         c.SourceServerConnection, c.DateFormat, c.CurrencySymbol,
                         c.CurrencyName, c.CurrencyCode, c.BaseURL, c.TimeZoneId, c.TaxEngine
                  FROM dbo.NomaroCountries c ORDER BY c.CountryName")).ToList();
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
                    await SeedCountryData(tenantConnection, country);
                    await ApplyCountryScreens(tenantConnection, country);
                    await Done(reg, SDbConfigured);
                }
                if (!Reached(reg, SAdminCreated))
                {
                    step = SAdminCreated;
                    var adminId = await CreateAdmin(tenantConnection, reg);
                    await SeedSalaryHeads(tenantConnection, country, adminId);
                    await SeedStatutoryRules(tenantConnection, country);
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
        private async Task<int> CreateAdmin(string connection, CompanyRegistration reg)
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
            return id.Value;
        }

        /// <summary>Country default salary heads (NOMARO_GLOBAL.dbo.CountrySalaryHeads) copied into the new company.
        /// A head whose code the company already has is left alone, so a retry is safe. Base heads are linked by code.</summary>
        private async Task SeedSalaryHeads(string connection, CountryRow country, int createdBy)
        {
            List<CountrySalaryHeadRow> heads;
            await using (var g = GlobalConnection())
            {
                heads = (await g.QueryAsync<CountrySalaryHeadRow>(
                    "SELECT * FROM dbo.CountrySalaryHeads WHERE CountryCode = @c ORDER BY ISNULL(CalcSequence, 9999), ISNULL(OrderNumber, 9999), SalaryHeadCode",
                    new { c = country.CountryCode.Trim() })).ToList();
            }
            if (heads.Count == 0)
            {
                _logger.LogWarning("No default salary heads in CountrySalaryHeads for country {Country}", country.CountryCode);
                return;
            }

            const string insertSql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.SalaryHeads WHERE SalaryHeadCode = @SalaryHeadCode)
BEGIN
    DECLARE @newId INT = NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.SalaryHeads'), 'IdSalaryHead', 'IsIdentity') <> 1
        SELECT @newId = ISNULL(MAX(IdSalaryHead), 0) + 1 FROM dbo.SalaryHeads WITH (UPDLOCK, HOLDLOCK);

    IF @newId IS NULL
        INSERT INTO dbo.SalaryHeads
            (SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, PercentageValue, FixedValue, CustomFormula,
             CreatedBy, CreatedOn, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
             IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
             IsProratedOnPaidDays, IsArrearHead, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence, RoundingRule, ShowOnPayslip)
        VALUES
            (@SalaryHeadCode, @SalaryHeadName, @HeadType, @IsTaxable, @IsActive, @CalculationMethod, @PercentageValue, @FixedValue, @CustomFormula,
             @CreatedBy, GETDATE(), @OrderNumber, @NonTaxableThreshold, @DisbursingMonths, @StatutoryType,
             @IsPartOfGross, @IsPartOfCTC, @IsPartOfPFWage, @IsPartOfESIWage, @IsPartOfGratuityWage, @IsPartOfPTWage,
             @IsProratedOnPaidDays, @IsArrearHead, @MinAmount, @MaxAmount, @WageCeiling, @PayFrequency, @CalcSequence, @RoundingRule, @ShowOnPayslip);
    ELSE
        INSERT INTO dbo.SalaryHeads
            (IdSalaryHead, SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, PercentageValue, FixedValue, CustomFormula,
             CreatedBy, CreatedOn, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
             IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
             IsProratedOnPaidDays, IsArrearHead, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence, RoundingRule, ShowOnPayslip)
        VALUES
            (@newId, @SalaryHeadCode, @SalaryHeadName, @HeadType, @IsTaxable, @IsActive, @CalculationMethod, @PercentageValue, @FixedValue, @CustomFormula,
             @CreatedBy, GETDATE(), @OrderNumber, @NonTaxableThreshold, @DisbursingMonths, @StatutoryType,
             @IsPartOfGross, @IsPartOfCTC, @IsPartOfPFWage, @IsPartOfESIWage, @IsPartOfGratuityWage, @IsPartOfPTWage,
             @IsProratedOnPaidDays, @IsArrearHead, @MinAmount, @MaxAmount, @WageCeiling, @PayFrequency, @CalcSequence, @RoundingRule, @ShowOnPayslip);
END";
            const string linkSql = @"
UPDATE s SET
    IdPercentageSalaryHead = CASE WHEN @PercentageOfHeadCode IS NULL THEN s.IdPercentageSalaryHead
                                  ELSE (SELECT TOP 1 b.IdSalaryHead FROM dbo.SalaryHeads b WHERE b.SalaryHeadCode = @PercentageOfHeadCode) END,
    IdBaseSalaryHead = CASE WHEN @BaseHeadCode IS NULL THEN s.IdBaseSalaryHead
                            ELSE (SELECT TOP 1 b.IdSalaryHead FROM dbo.SalaryHeads b WHERE b.SalaryHeadCode = @BaseHeadCode) END
FROM dbo.SalaryHeads s
WHERE s.SalaryHeadCode = @SalaryHeadCode AND s.CreatedBy = @CreatedBy;";

            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            foreach (var h in heads)
            {
                var p = new DynamicParameters(h);
                p.Add("CreatedBy", createdBy);
                await cn.ExecuteAsync(insertSql, p, tx, commandTimeout: 120);
            }
            foreach (var h in heads.Where(h => !string.IsNullOrWhiteSpace(h.PercentageOfHeadCode) || !string.IsNullOrWhiteSpace(h.BaseHeadCode)))
            {
                await cn.ExecuteAsync(linkSql, new
                {
                    h.SalaryHeadCode,
                    PercentageOfHeadCode = string.IsNullOrWhiteSpace(h.PercentageOfHeadCode) ? null : h.PercentageOfHeadCode.Trim(),
                    BaseHeadCode = string.IsNullOrWhiteSpace(h.BaseHeadCode) ? null : h.BaseHeadCode.Trim(),
                    CreatedBy = createdBy
                }, tx, commandTimeout: 120);
            }
            await tx.CommitAsync();
        }

        /// <summary>Screens shown per country (NOMARO_GLOBAL.dbo.CountryScreenConfig) -> PayrollScreens.Enabled of the new company.
        /// Screens not listed keep their value from the source database.</summary>
        private async Task ApplyCountryScreens(string connection, CountryRow country)
        {
            List<(string ScreenCode, bool Enabled)> screens;
            await using (var g = GlobalConnection())
            {
                screens = (await g.QueryAsync<(string ScreenCode, bool Enabled)>(@"
IF OBJECT_ID('dbo.CountryScreenConfig', 'U') IS NOT NULL
    SELECT ScreenCode, Enabled FROM dbo.CountryScreenConfig WHERE CountryCode = @code;", new { code = country.CountryCode.Trim() })).ToList();
            }
            if (screens.Count == 0) return;

            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            foreach (var screen in screens)
            {
                await cn.ExecuteAsync("UPDATE dbo.PayrollScreens SET Enabled = @Enabled WHERE ScreenCode = @ScreenCode",
                    new { screen.ScreenCode, screen.Enabled });
            }
            _logger.LogInformation("Company screens set for country {Country}: {Count} rules", country.CountryCode, screens.Count);
        }

        private static readonly Regex TableNamePattern = new("^[A-Za-z0-9_]{1,128}$", RegexOptions.Compiled);

        /// <summary>Country master data (NOMARO_GLOBAL.dbo.CountryDataTables / cd.&lt;table&gt;) copied into the new company with
        /// the same Ids, so links between the tables stay right. A table that already has rows in the company is skipped,
        /// so a retry is safe. Only columns present in both tables are copied.</summary>
        private async Task SeedCountryData(string connection, CountryRow country)
        {
            var code = country.CountryCode.Trim();
            List<string> tables;
            await using (var g = GlobalConnection())
            {
                tables = (await g.QueryAsync<string>(@"
IF OBJECT_ID('dbo.CountryDataTables', 'U') IS NOT NULL
    SELECT TableName FROM dbo.CountryDataTables WHERE IsActive = 1 ORDER BY CopyOrder, TableName;")).ToList();
            }
            if (tables.Count == 0) return;

            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
            await using var globalCn = GlobalConnection();
            await globalCn.OpenAsync();

            const string columnsSql = @"
SELECT c.name FROM sys.columns c
WHERE c.object_id = OBJECT_ID(@table) AND c.is_computed = 0 AND TYPE_NAME(c.system_type_id) NOT IN ('timestamp', 'rowversion')";

            foreach (var table in tables.Where(t => TableNamePattern.IsMatch(t)))
            {
                var source = $"cd.[{table}]";
                var target = $"dbo.[{table}]";

                var sourceColumns = (await globalCn.QueryAsync<string>(columnsSql, new { table = source })).ToList();
                if (sourceColumns.Count == 0) continue;   // not captured for any country yet

                var targetColumns = (await cn.QueryAsync<string>(columnsSql, new { table = target }, tx)).ToList();
                if (targetColumns.Count == 0)
                {
                    _logger.LogWarning("Country data {Table}: not in the new company database, skipped", table);
                    continue;
                }
                if (await cn.ExecuteScalarAsync<int?>($"SELECT TOP 1 1 FROM {target}", transaction: tx) == 1) continue;   // already has data

                var columns = targetColumns
                    .Where(t => !t.Equals("CountryCode", StringComparison.OrdinalIgnoreCase)
                                && sourceColumns.Any(s => s.Equals(t, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (columns.Count == 0) continue;

                var data = new DataTable();
                await using (var cmd = new SqlCommand(
                    $"SELECT {string.Join(", ", columns.Select(c => "[" + c + "]"))} FROM {source} WHERE CountryCode = @code", globalCn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    data.Load(reader);
                }
                if (data.Rows.Count == 0) continue;

                using var bulk = new SqlBulkCopy(cn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, tx)
                {
                    DestinationTableName = target,
                    BulkCopyTimeout = SqlTimeoutSeconds
                };
                foreach (var c in columns) bulk.ColumnMappings.Add(c, c);
                await bulk.WriteToServerAsync(data);
                _logger.LogInformation("Country data {Table}: {Count} rows copied for {Country}", table, data.Rows.Count, code);
            }
            await tx.CommitAsync();
        }

        /// <summary>Country statutory rules, PT / LWF slabs (NOMARO_GLOBAL) and the tax engine setting copied to the new company,
        /// for the generic salary procedure. Existing rows are kept, so a retry is safe.</summary>
        private async Task SeedStatutoryRules(string connection, CountryRow country)
        {
            var code = country.CountryCode.Trim();
            IEnumerable<dynamic> rules, pt, lwf;
            await using (var g = GlobalConnection())
            {
                rules = await g.QueryAsync("SELECT * FROM dbo.CountryStatutoryRules WHERE CountryCode = @code", new { code });
                pt = await g.QueryAsync("SELECT * FROM dbo.CountryPTSlabs WHERE CountryCode = @code", new { code });
                lwf = await g.QueryAsync("SELECT * FROM dbo.CountryLWFSlabs WHERE CountryCode = @code", new { code });
            }

            await using var cn = new SqlConnection(connection);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            foreach (var r in rules)
            {
                await cn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM dbo.StatutoryRules WHERE StatutoryType = @StatutoryType AND EffectiveFrom = @EffectiveFrom)
    INSERT INTO dbo.StatutoryRules (StatutoryType, Description, RateType, Rate, RateThreshold, RateAboveThreshold, WageBase, WageCeiling,
        CeilingWaivedBy, MinWage, EligibilityWageLimit, EligibilityWageLimitDisabled, MinAge, MaxAge, EmployeeFlag, DifferenceOfType,
        MaxAmount, EffectiveFrom, EffectiveTo, IsActive)
    VALUES (@StatutoryType, @Description, @RateType, @Rate, @RateThreshold, @RateAboveThreshold, @WageBase, @WageCeiling,
        @CeilingWaivedBy, @MinWage, @EligibilityWageLimit, @EligibilityWageLimitDisabled, @MinAge, @MaxAge, @EmployeeFlag, @DifferenceOfType,
        @MaxAmount, @EffectiveFrom, @EffectiveTo, @IsActive);",
                    new
                    {
                        StatutoryType = (string)r.StatutoryType, Description = (string?)r.Description, RateType = (string)r.RateType,
                        Rate = (decimal?)r.Rate, RateThreshold = (decimal?)r.RateThreshold, RateAboveThreshold = (decimal?)r.RateAboveThreshold,
                        WageBase = (string)r.WageBase, WageCeiling = (decimal?)r.WageCeiling, CeilingWaivedBy = (string?)r.CeilingWaivedBy,
                        MinWage = (decimal?)r.MinWage, EligibilityWageLimit = (decimal?)r.EligibilityWageLimit,
                        EligibilityWageLimitDisabled = (decimal?)r.EligibilityWageLimitDisabled, MinAge = (int?)r.MinAge, MaxAge = (int?)r.MaxAge,
                        EmployeeFlag = (string?)r.EmployeeFlag, DifferenceOfType = (string?)r.DifferenceOfType, MaxAmount = (decimal?)r.MaxAmount,
                        EffectiveFrom = (DateTime)r.EffectiveFrom, EffectiveTo = (DateTime?)r.EffectiveTo, IsActive = (bool)r.IsActive
                    }, tx);
            }
            foreach (var r in pt)
            {
                await cn.ExecuteAsync(@"
INSERT INTO dbo.PTSlabs (IdState, Gender, SalaryFrom, SalaryTo, PTAmount, ApplicableMonth, DeductionFrequency, WageBasis, EffectiveFrom, EffectiveTo, IsActive)
SELECT st.IdState, @Gender, @SalaryFrom, @SalaryTo, @PTAmount, @ApplicableMonth, @DeductionFrequency, @WageBasis, @EffectiveFrom, @EffectiveTo, @IsActive
FROM dbo.States st
WHERE st.StateCode = @StateCode
  AND NOT EXISTS (SELECT 1 FROM dbo.PTSlabs p WHERE p.IdState = st.IdState AND ISNULL(p.Gender, '') = ISNULL(@Gender, '')
                  AND p.SalaryFrom = @SalaryFrom AND ISNULL(p.ApplicableMonth, 0) = ISNULL(@ApplicableMonth, 0)
                  AND p.EffectiveFrom = @EffectiveFrom);",
                    new
                    {
                        StateCode = (string)r.StateCode, Gender = (string?)r.Gender, SalaryFrom = (decimal)r.SalaryFrom, SalaryTo = (decimal?)r.SalaryTo,
                        PTAmount = (decimal)r.PTAmount, ApplicableMonth = NullableInt((object)r.ApplicableMonth),
                        DeductionFrequency = (string)r.DeductionFrequency, WageBasis = (string)r.WageBasis,
                        EffectiveFrom = (DateTime)r.EffectiveFrom, EffectiveTo = (DateTime?)r.EffectiveTo, IsActive = (bool)r.IsActive
                    }, tx);
            }
            foreach (var r in lwf)
            {
                await cn.ExecuteAsync(@"
INSERT INTO dbo.LWFSlabs (IdState, SalaryFrom, SalaryTo, EmployeeAmount, EmployerAmount, DeductionMonths, EffectiveFrom, EffectiveTo, IsActive)
SELECT st.IdState, @SalaryFrom, @SalaryTo, @EmployeeAmount, @EmployerAmount, @DeductionMonths, @EffectiveFrom, @EffectiveTo, @IsActive
FROM dbo.States st
WHERE st.StateCode = @StateCode
  AND NOT EXISTS (SELECT 1 FROM dbo.LWFSlabs l WHERE l.IdState = st.IdState AND ISNULL(l.SalaryFrom, 0) = ISNULL(@SalaryFrom, 0)
                  AND l.EffectiveFrom = @EffectiveFrom);",
                    new
                    {
                        StateCode = (string)r.StateCode, SalaryFrom = (decimal?)r.SalaryFrom, SalaryTo = (decimal?)r.SalaryTo, EmployeeAmount = (decimal)r.EmployeeAmount,
                        EmployerAmount = (decimal)r.EmployerAmount, DeductionMonths = (string)r.DeductionMonths, EffectiveFrom = (DateTime)r.EffectiveFrom,
                        EffectiveTo = (DateTime?)r.EffectiveTo, IsActive = (bool)r.IsActive
                    }, tx);
            }

            var engine = string.IsNullOrWhiteSpace(country.TaxEngine) ? "NONE" : country.TaxEngine.Trim().ToUpperInvariant();
            var p = new DynamicParameters();
            p.Add("name", "TaxEngine", DbType.String);
            p.Add("description", "Income tax engine: NONE or IN_TDS", DbType.String);
            p.Add("value", engine, DbType.String);
            p.Add("binary", null, DbType.Binary, size: -1);
            p.Add("setValue", true, DbType.Boolean);
            p.Add("setBinary", false, DbType.Boolean);
            p.Add("dataType", "STRING", DbType.String);
            p.Add("show", false, DbType.Boolean);
            await cn.ExecuteAsync(UpsertParameterSql, p, tx);

            await tx.CommitAsync();
        }

        private static int? NullableInt(object? value) => value == null || value is DBNull ? null : Convert.ToInt32(value);

        private sealed class CountrySalaryHeadRow
        {
            public string SalaryHeadCode { get; set; } = "";
            public string SalaryHeadName { get; set; } = "";
            public string HeadType { get; set; } = "";
            public bool IsTaxable { get; set; }
            public bool IsActive { get; set; }
            public string CalculationMethod { get; set; } = "";
            public string? PercentageOfHeadCode { get; set; }
            public decimal? PercentageValue { get; set; }
            public decimal? FixedValue { get; set; }
            public string? CustomFormula { get; set; }
            public int? OrderNumber { get; set; }
            public decimal? NonTaxableThreshold { get; set; }
            public string? DisbursingMonths { get; set; }
            public string? StatutoryType { get; set; }
            public bool IsPartOfGross { get; set; }
            public bool IsPartOfCTC { get; set; }
            public bool IsPartOfPFWage { get; set; }
            public bool IsPartOfESIWage { get; set; }
            public bool IsPartOfGratuityWage { get; set; }
            public bool IsPartOfPTWage { get; set; }
            public bool IsProratedOnPaidDays { get; set; }
            public bool IsArrearHead { get; set; }
            public string? BaseHeadCode { get; set; }
            public decimal? MinAmount { get; set; }
            public decimal? MaxAmount { get; set; }
            public decimal? WageCeiling { get; set; }
            public string? PayFrequency { get; set; }
            public int? CalcSequence { get; set; }
            public string? RoundingRule { get; set; }
            public bool ShowOnPayslip { get; set; }
        }

        /// <summary>Last step: the NomaroInstanceConfig row makes the company reachable.</summary>
        private async Task Activate(CompanyRegistration reg, CountryRow country, string connection)
        {
            // the new database must answer before it is published
            await using (var cn = new SqlConnection(connection))
                await cn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.SystemParameters");

            await SeedScreenAttributeConfig(reg.CompanyCode, country.CountryCode.Trim());

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

        /// <summary>Field labels / visibility of the country (CountryScreenAttributeConfig) copied to the company
        /// (CompanyScreenAttributeConfig). Rows the company already has are kept, so a retry is safe.</summary>
        private async Task SeedScreenAttributeConfig(string companyCode, string countryCode)
        {
            const string sql = @"
IF COLUMNPROPERTY(OBJECT_ID('dbo.CompanyScreenAttributeConfig'), 'IdCompanyScreenAttributeConfig', 'IsIdentity') = 1
    INSERT INTO dbo.CompanyScreenAttributeConfig (CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
    SELECT @companyCode, c.ScreenCode, c.AttributeID, c.DisplayLabel, c.FieldVisibility, c.MaxLength, c.DataType
    FROM dbo.CountryScreenAttributeConfig c
    WHERE c.CountryCode = @countryCode
      AND NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig x
                      WHERE x.CompanyCode = @companyCode AND x.ScreenCode = c.ScreenCode AND x.AttributeID = c.AttributeID);
ELSE
    INSERT INTO dbo.CompanyScreenAttributeConfig (IdCompanyScreenAttributeConfig, CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
    SELECT (SELECT ISNULL(MAX(IdCompanyScreenAttributeConfig), 0) FROM dbo.CompanyScreenAttributeConfig WITH (UPDLOCK, HOLDLOCK))
               + ROW_NUMBER() OVER (ORDER BY c.ScreenCode, c.AttributeID),
           @companyCode, c.ScreenCode, c.AttributeID, c.DisplayLabel, c.FieldVisibility, c.MaxLength, c.DataType
    FROM dbo.CountryScreenAttributeConfig c
    WHERE c.CountryCode = @countryCode
      AND NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig x
                      WHERE x.CompanyCode = @companyCode AND x.ScreenCode = c.ScreenCode AND x.AttributeID = c.AttributeID);
SELECT @@ROWCOUNT;";
            await using var cn = GlobalConnection();
            var added = await cn.ExecuteScalarAsync<int>(sql, new { companyCode, countryCode }, commandTimeout: 120);
            _logger.LogInformation("Company {Code}: {Count} field settings copied from country {Country}", companyCode, added, countryCode);
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
