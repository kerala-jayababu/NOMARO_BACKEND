using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Nomaro.API.Services.Interface.Time___Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nomaro.API.Services.Implimentation.Time___Attendance
{
    public class SmsService : ISmsService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<SmsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly INotificationConfigService _notificationConfigService;

        public SmsService(
            ApplicationDBContext dbContext,
            IHttpClientFactory httpClientFactory,
            ILogger<SmsService> logger,
            IConfiguration configuration,
            INotificationConfigService notificationConfigService)
        {
            _dbContext = dbContext;
            _logger = logger;
           _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _notificationConfigService = notificationConfigService;
        }

        public async Task<bool> SendSmsMissingEntryAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime exitDateTime)
        {
            const string entityCode = "SMS_MISSING_ENTRY";
            return await SendSmsAndSaveNotification(entityCode, idEmployee, loggedInEmployeeId, mobileNumber, employeeName, exitDateTime);
        }

        public async Task<bool> SendSmsMissingExitAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime entryDateTime)
        {
            const string entityCode = "SMS_MISSING_EXIT";
            return await SendSmsAndSaveNotification(entityCode, idEmployee, loggedInEmployeeId, mobileNumber, employeeName, entryDateTime);
        }

        public async Task<bool> SendSmsUnauthorizedAbsenceAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime absentDate)
        {
            const string entityCode = "SMS_UNAUTHORIZED_ABSENCE";
            return await SendSmsAndSaveNotification(entityCode, idEmployee, loggedInEmployeeId, mobileNumber, employeeName, absentDate);
        }

        private async Task<bool> SendSmsAndSaveNotification(string entityCode,int idEmployee,int loggedInEmployeeId,string mobileNumber,string employeeName,DateTime dt)
        {
            if (idEmployee <= 0)
            {
                _logger.LogWarning("Invalid idEmployee.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                _logger.LogWarning("MobileNumber is empty for idEmployee: {IdEmployee}", idEmployee);
                return false;
            }

            var notificationConfig = await GetNotificationConfigForSmsEntity(entityCode, employeeName ?? "", dt);
            if (notificationConfig == null || string.IsNullOrWhiteSpace(notificationConfig.EmailContent))
            {
                _logger.LogWarning("No NotificationConfig found for EntityCode: {EntityCode}", entityCode);
                return false;
            }

            var smsText = HtmlToSmsText(notificationConfig.EmailContent);

            // ✅ Always await
            await SendSMSAsync(mobileNumber, smsText);

            // ✅ Save Notifications table with ReceivedByIdEmployee = idEmployee
            await InsertNotificationAsync(
                notificationConfig: notificationConfig,
                loggedInEmployeeId: loggedInEmployeeId,
                receivedByEmployeeId: idEmployee,
                entityCode: entityCode,
                relatedRecordId: null);

            return true;
        }

        private async Task<NotificationConfigDto?> GetNotificationConfigForSmsEntity(string entityCode, string receiverName, DateTime datetimeValue)
        {
            var cfg = await _dbContext.NotificationsConfig
                .Where(n => n.EntityCode == entityCode)
                .FirstOrDefaultAsync();

            if (cfg == null || (string.IsNullOrWhiteSpace(cfg.EmailContent) && cfg.TemplateSelection != "HTMLFILE"))
                return null;

            var dtText = datetimeValue.ToString("dd-MMM-yyyy hh:mm tt");

            // Build replacement dictionary
            var replacements = new Dictionary<string, string>
            {
                { "#RECEIVER#", receiverName ?? "" },
                { "#DATETIMME#", dtText },
                { "#CURRENTDATETIME#", dtText }
            };

            // Get processed content using helper method
            string processedContent = await _notificationConfigService.GetProcessedNotificationContentAsync(cfg, replacements);

            return new NotificationConfigDto
            {
                IdNotificationConfig = cfg.IdNotificationConfig,
                NotificationType = cfg.NotificationType,
                EntityCode = cfg.EntityCode,
                EmailSubject = cfg.EmailSubject,
                EmailContent = processedContent,
                LogoText = cfg.LogoText,
                NotificationLink = null,
                AppNotificationText = processedContent,
                WebLink = cfg.WebLink
            };
        }

        private async Task InsertNotificationAsync(
            NotificationConfigDto notificationConfig,
            int loggedInEmployeeId,
            int? receivedByEmployeeId,
            string entityCode,
            int? relatedRecordId)
        {
            var obj = new Notification
            {
                IdNotificationConfig = notificationConfig.IdNotificationConfig,
                NotificationType = notificationConfig.NotificationType,

                SentByIdEmployee = loggedInEmployeeId,
                ReceivedByIdEmployee = receivedByEmployeeId,     // ✅ null allowed only if column nullable

                AppNotificationText = notificationConfig.AppNotificationText,
                EmailSubject = notificationConfig.EmailSubject,
                EmailContent = notificationConfig.EmailContent,

                EmailSentStatus = "SENT",
                IsReadAppNotification = true,
                CreatedAt = DateTime.Now,
                Status = "SENT",
                ReadAt = DateTime.Now,

                RelatedRecordID = relatedRecordId,
                RelatedRecordType = entityCode,                  // ✅ should store entityCode not null

                LogoText = notificationConfig.LogoText,
                NotificationLink = notificationConfig.NotificationLink
            };

            await _dbContext.Notifications.AddAsync(obj);
            await _dbContext.SaveChangesAsync();
        }

        private static string HtmlToSmsText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            return html
                .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
                .Replace("</p>", "\n").Replace("<p>", "")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Trim();
        }

        public async Task SendSMSAsync(string mobileNumber, string message)
        {
            string apiUrl = _configuration["SMSALA:ApiUrl"] ?? "https://api2.smsala.com/SendSmsV2";
            string apiToken = _configuration["SMSALA:ApiToken"] ?? "yBIbRPyFypoPfGme";
            string senderId = _configuration["SMSALA:SenderId"] ?? "GIA";

            string url =
                apiUrl +
                "?apiToken=" + Uri.EscapeDataString(apiToken) +
                "&messageType=1" +
                "&messageEncoding=1" +
                "&destinationAddress=" + Uri.EscapeDataString(mobileNumber) +
                "&sourceAddress=" + Uri.EscapeDataString(senderId) +
                "&messageText=" + Uri.EscapeDataString(message) +
                "&userReferenceId=" + Guid.NewGuid().ToString("N");

            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response = await client.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("HTTP Error: " + response.StatusCode + " | " + result);

            if (!result.Contains("\"Status\":\"Success\""))
                throw new Exception("SMSALA Error: " + result);
        }

        public async Task SendSMSOTP(string mobileNumber, string OTP)
        {
            string message = "Your OTP for Login to Self Portal is " + OTP;
            SendSMSAsync(mobileNumber, message);
        }
    }

    public class SendSMSOTP
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SendSMSOTP(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendSMSAsync(string mobileNumber, string message)
        {
            string apiUrl = _configuration["SMSALA:ApiUrl"];
            string apiToken = _configuration["SMSALA:ApiToken"];
            string senderId = _configuration["SMSALA:SenderId"];

            if (string.IsNullOrWhiteSpace(apiUrl) ||
                string.IsNullOrWhiteSpace(apiToken) ||
                string.IsNullOrWhiteSpace(senderId))
            {
                throw new InvalidOperationException("SMSALA configuration is missing in appsettings.json");
            }

            string url =
                $"{apiUrl}" +
                $"?apiToken={Uri.EscapeDataString(apiToken)}" +
                $"&messageType=1" +
                $"&messageEncoding=1" +
                $"&destinationAddress={Uri.EscapeDataString(mobileNumber)}" +
                $"&sourceAddress={Uri.EscapeDataString(senderId)}" +
                $"&messageText={Uri.EscapeDataString(message)}" +
                $"&userReferenceId={Guid.NewGuid():N}";

            var client = _httpClientFactory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP Error: {response.StatusCode} | {result}");

            if (!result.Contains("\"Status\":\"Success\"", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"SMSALA Error: {result}");
        }
    }

}

