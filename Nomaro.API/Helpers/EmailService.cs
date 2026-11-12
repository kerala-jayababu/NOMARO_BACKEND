using System.Net.Mail;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Helpers
{
    public static class EmailService
    {
        private class EmailSettings
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string SmtpHost { get; set; }
            public int SmtpPort { get; set; }
            public bool EnableSsl { get; set; }
            public string DisplayName { get; set; }
        }

        // Fallback settings from appsettings.json, used only when the tenant database
        // has no "EmailSettings" system parameter.
        private static readonly EmailSettings _fallbackSettings;

        // Set once at startup from Program.cs so the static helpers can resolve the
        // current request's tenant connection string.
        private static IServiceProvider? _serviceProvider;

        static EmailService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _fallbackSettings = config
                .GetSection("EmailSettings")
                .Get<EmailSettings>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Loads EmailSettings from the current tenant database's SystemParameters table
        // (ParameterName = 'EmailSettings', ParameterValue = JSON). Falls back to
        // appsettings.json when the parameter is missing or unreadable.
        private static async Task<EmailSettings> GetSettingsAsync()
        {
            try
            {
                var connectionString = ResolveTenantConnectionString();
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    using var connection = new SqlConnection(connectionString);
                    var json = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT TOP 1 ParameterValue FROM SystemParameters WHERE ParameterName = 'EmailSettings'");

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = ParseEmailSettings(json);
                        if (settings != null && !string.IsNullOrWhiteSpace(settings.SmtpHost))
                        {
                            return settings;
                        }
                    }
                }
            }
            catch
            {
                // fall through to appsettings.json fallback
            }
            return _fallbackSettings;
        }

        // The stored ParameterValue may be either the raw settings object
        //   {"Email":"...","SmtpHost":"...",...}
        // or wrapped with the section key
        //   {"EmailSettings":{"Email":"...",...}}  /  "EmailSettings": {...}
        private static EmailSettings? ParseEmailSettings(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var trimmed = json.Trim();

            // Normalize a leading "EmailSettings": { ... } wrapper into a full JSON object
            if (trimmed.StartsWith("\"EmailSettings\"", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "{" + trimmed.TrimEnd().TrimEnd(',') + "}";
            }

            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            // Unwrap { "EmailSettings": { ... } } if present
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("EmailSettings", out var inner) &&
                inner.ValueKind == JsonValueKind.Object)
            {
                root = inner;
            }

            return JsonSerializer.Deserialize<EmailSettings>(root.GetRawText(), options);
        }

        private static string? ResolveTenantConnectionString()
        {
            if (_serviceProvider == null) return null;

            // Prefer the current request's scoped tenant (set by TenantMiddleware).
            var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
            var requestServices = httpContextAccessor?.HttpContext?.RequestServices;
            if (requestServices != null)
            {
                var tenantService = requestServices.GetService<ITenantService>();
                return tenantService?.ResolveConnectionString();
            }

            // No active request (background task): create a scope to check for a tenant context.
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<ITenantService>()?.ResolveConnectionString();
        }

        public static async Task<bool> SendMail(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var settings = await GetSettingsAsync();
                using (var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort))
                {
                    client.EnableSsl = settings.EnableSsl;
                    client.Credentials = new NetworkCredential(settings.Email, settings.Password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(settings.Email, settings.DisplayName ?? settings.Email),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static async Task<bool> SendMail(
       string toEmail,
       string subject,
       string htmlBody,
       byte[] attachmentBytes,
       string attachmentFileName)
        {
            try
            {
                var settings = await GetSettingsAsync();
                using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.Email, settings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.Email, settings.DisplayName ?? settings.Email),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                // attach the PDF
                using var pdfStream = new MemoryStream(attachmentBytes);
                var attachment = new Attachment(pdfStream, attachmentFileName,
                                                MediaTypeNames.Application.Pdf);
                mailMessage.Attachments.Add(attachment);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                // consider logging
                throw;
            }
        }

        public static async Task<bool> SendMailWithCcToMany(IEnumerable<string> toEmails,
                IEnumerable<string>? ccEmails, string subject, string htmlBody)
        {
            try
            {
                var settings = await GetSettingsAsync();
                using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.Email, settings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.Email, settings.DisplayName ?? settings.Email),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                foreach (var to in (toEmails ?? Enumerable.Empty<string>())
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    mailMessage.To.Add(to);
                }

                foreach (var cc in (ccEmails ?? Enumerable.Empty<string>())
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    mailMessage.CC.Add(cc);
                }

                if (mailMessage.To.Count == 0) return false;

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                throw;
            }
        }


    }
}
