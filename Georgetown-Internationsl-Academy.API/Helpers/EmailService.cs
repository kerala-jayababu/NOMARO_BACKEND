using System.Net.Mail;
using System.Net;
using System.Net.Mime;

namespace Georgetown_Internationsl_Academy.API.Helpers
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
        private static readonly EmailSettings _settings;

        static EmailService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())     
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _settings = config
                .GetSection("EmailSettings")
                .Get<EmailSettings>();
        }
        public static async Task<bool> SendMail(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using (var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort))
                {
                    client.EnableSsl = _settings.EnableSsl;
                    client.Credentials = new NetworkCredential(_settings.Email, _settings.Password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_settings.Email, _settings.DisplayName ?? _settings.Email),
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
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Email, _settings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.Email, _settings.DisplayName ?? _settings.Email),
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

        public static async Task<bool> SendMailWithCcToMany(
    IEnumerable<string> toEmails,
    IEnumerable<string>? ccEmails,
    string subject,
    string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Email, _settings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.Email, _settings.DisplayName ?? _settings.Email),
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
