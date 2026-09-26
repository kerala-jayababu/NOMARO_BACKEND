using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Nomaro.API.DTO
{
    public class NotificationConfigDto
    {
        public int IdNotificationConfig { get; set; }
        public string NotificationType { get; set; }
        public string? EmailSubject { get; set; }
        public string? EntityCode { get; set; }
        public string? EmailContent { get; set; }
        public string? AppNotificationText { get; set; }
        public string? WebLink { get; set; }
        public string? TemplateSelection { get; set; }
        public string? HTMLTemplatePath { get; set; }

        /// <summary>Multipart update only. Not stored in DB.</summary>
        [NotMapped]
        public IFormFile? HtmlTemplateFile { get; set; }

        /// <summary>Populated on GET when <see cref="HTMLTemplatePath"/> points to an existing file.</summary>
        [NotMapped]
        public byte[]? HtmlTemplateFileContent { get; set; }

        /// <summary>Populated on GET together with <see cref="HtmlTemplateFileContent"/>.</summary>
        [NotMapped]
        public string? HtmlTemplateFileName { get; set; }

        [NotMapped]
        public string? LogoText { get; set; }

        [NotMapped]
        public string? NotificationLink { get; set; }
    }
}

