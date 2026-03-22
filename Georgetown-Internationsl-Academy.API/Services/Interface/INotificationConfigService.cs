using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface INotificationConfigService
    {
        Task<IEnumerable<NotificationConfigDto>> GetNotificationConfigList(bool includeHtmlTemplateFileContent = true);
        Task<NotificationConfigDto?> GetNotificationConfigById(int id, bool includeHtmlTemplateFileContent = true);
        Task<NotificationConfigDto> AddNotificationConfig(NotificationConfigDto dto);
        Task<NotificationConfigDto> UpdateNotificationConfig(NotificationConfigDto dto);
        Task<string?> GetProcessedNotificationContentAsync(NotificationConfig config, Dictionary<string, string> replacements);
    }
}
