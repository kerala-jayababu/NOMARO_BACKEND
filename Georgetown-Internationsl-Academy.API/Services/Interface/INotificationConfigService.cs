using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface INotificationConfigService
    {
        Task<IEnumerable<NotificationConfigDto>> GetNotificationConfigList();
        Task<NotificationConfigDto> GetNotificationConfigById(int id);
        Task<NotificationConfigDto> AddNotificationConfig(NotificationConfigDto dto);
        Task<NotificationConfigDto> UpdateNotificationConfig(NotificationConfigDto dto);
    }
}
