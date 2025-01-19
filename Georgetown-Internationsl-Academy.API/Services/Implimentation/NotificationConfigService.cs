using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class NotificationConfigService : INotificationConfigService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationConfigService> _logger;

        public NotificationConfigService(ApplicationDBContext dbContext, IMapper mapper, ILogger<NotificationConfigService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationConfigDto>> GetNotificationConfigList()
        {
            try
            {
                var notificationConfigs = await _dbContext.NotificationsConfig.ToListAsync();
                return _mapper.Map<IEnumerable<NotificationConfigDto>>(notificationConfigs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of notification configurations.");
                throw;
            }
        }

        public async Task<NotificationConfigDto?> GetNotificationConfigById(int id)
        {
            try
            {
                var notificationConfig = await _dbContext.NotificationsConfig.FirstOrDefaultAsync(n => n.IdNotificationConfig == id);
                return notificationConfig == null ? null : _mapper.Map<NotificationConfigDto>(notificationConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notification configuration with ID: {Id}", id);
                throw;
            }
        }

        public async Task<NotificationConfigDto?> AddNotificationConfig(NotificationConfigDto dto)
        {
            try
            {
                var notificationConfigEntity = _mapper.Map<NotificationConfig>(dto);
                var addedEntity = await _dbContext.NotificationsConfig.AddAsync(notificationConfigEntity);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<NotificationConfigDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification configuration: {@NotificationConfigDto}", dto);
                return null;
            }
        }

        public async Task<NotificationConfigDto?> UpdateNotificationConfig(NotificationConfigDto dto)
        {
            try
            {
                var notificationConfig = await _dbContext.NotificationsConfig
                    .FirstOrDefaultAsync(n => n.IdNotificationConfig == dto.IdNotificationConfig);

                if (notificationConfig == null) return null;

                // Update properties
                notificationConfig.NotificationType = dto.NotificationType;
                notificationConfig.EmailSubject = dto.EmailSubject;
                notificationConfig.EmailContent = dto.EmailContent;
                notificationConfig.AppNotificationText = dto.AppNotificationText;
                notificationConfig.WebLink = dto.WebLink;

                var updatedEntity = _dbContext.NotificationsConfig.Update(notificationConfig);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<NotificationConfigDto>(updatedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification configuration with ID: {Id}", dto.IdNotificationConfig);
                return null;
            }
        }
    }
}
