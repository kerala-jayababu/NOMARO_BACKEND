using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nomaro.API.Services.Implimentation
{
    public class NotificationConfigService : INotificationConfigService
    {
        private const string NotificationTemplatesSubFolder = "Uploads/NotificationTemplates";

        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationConfigService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;

        public NotificationConfigService(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<NotificationConfigService> logger,
            IWebHostEnvironment webHostEnvironment,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
        }

        public async Task<IEnumerable<NotificationConfigDto>> GetNotificationConfigList(bool includeHtmlTemplateFileContent = true)
        {
            try
            {
                var notificationConfigs = await _dbContext.NotificationsConfig.OrderBy(n=>n.NotificationType).ToListAsync();
                var list = _mapper.Map<List<NotificationConfigDto>>(notificationConfigs);
                if (includeHtmlTemplateFileContent)
                {
                    foreach (var dto in list)
                    {
                        await HydrateHtmlTemplateAsync(dto);
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of notification configurations.");
                throw;
            }
        }

        public async Task<NotificationConfigDto?> GetNotificationConfigById(int id, bool includeHtmlTemplateFileContent = true)
        {
            try
            {
                var notificationConfig = await _dbContext.NotificationsConfig.FirstOrDefaultAsync(n => n.IdNotificationConfig == id);
                if (notificationConfig == null)
                {
                    return null;
                }

                var dto = _mapper.Map<NotificationConfigDto>(notificationConfig);
                if (includeHtmlTemplateFileContent)
                {
                    await HydrateHtmlTemplateAsync(dto);
                }

                return dto;
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

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "NotificationConfig",
                    entityId: addedEntity.Entity.IdNotificationConfig,
                    actionDetails: new { after = addedEntity.Entity });

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
                notificationConfig.TemplateSelection = dto.TemplateSelection;
                notificationConfig.AppNotificationText = dto.AppNotificationText;
                notificationConfig.WebLink = dto.WebLink;

                if (dto.HtmlTemplateFile is { Length: > 0 })
                {
                    var ext = Path.GetExtension(dto.HtmlTemplateFile.FileName)?.ToLowerInvariant();
                    if (ext != ".html" && ext != ".htm")
                    {
                        throw new InvalidOperationException("HTML template must be a .html or .htm file.");
                    }

                    var uploadFolder = Path.Combine(_webHostEnvironment.ContentRootPath, NotificationTemplatesSubFolder);
                    Directory.CreateDirectory(uploadFolder);

                    var previousPath = notificationConfig.HTMLTemplatePath;
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dto.HtmlTemplateFileName); // Get file name without extension
                    var safeName = $"{dto.IdNotificationConfig}_{Guid.NewGuid():N}_{fileNameWithoutExtension}{ext}"; // Combine GUID, file name, and extension
                    var fullPath = Path.Combine(uploadFolder, safeName);                    
                    await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                    {
                        await dto.HtmlTemplateFile.CopyToAsync(stream);
                    }

                    notificationConfig.HTMLTemplatePath = fullPath;

                    if (!string.IsNullOrWhiteSpace(previousPath)
                        && !previousPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)
                        && File.Exists(previousPath))
                    {
                        try
                        {
                            File.Delete(previousPath);
                        }
                        catch (Exception delEx)
                        {
                            _logger.LogWarning(delEx, "Could not delete previous HTML template file: {Path}", previousPath);
                        }
                    }
                }

                var beforeUpdate = _mapper.Map<NotificationConfigDto>(notificationConfig);

                var updatedEntity = _dbContext.NotificationsConfig.Update(notificationConfig);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "NotificationConfig",
                    entityId: updatedEntity.Entity.IdNotificationConfig,
                    actionDetails: new { before = beforeUpdate, after = _mapper.Map<NotificationConfigDto>(updatedEntity.Entity) });

                var result = _mapper.Map<NotificationConfigDto>(updatedEntity.Entity);
                await HydrateHtmlTemplateAsync(result);
                result.HtmlTemplateFile = null;
                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification configuration with ID: {Id}", dto.IdNotificationConfig);
                return null;
            }
        }

        private async Task HydrateHtmlTemplateAsync(NotificationConfigDto dto)
        {
            dto.HtmlTemplateFileContent = null;
            dto.HtmlTemplateFileName = null;

            if (string.IsNullOrWhiteSpace(dto.HTMLTemplatePath))
            {
                return;
            }

            var path = dto.HTMLTemplatePath.Trim();
            try
            {
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(
                        _webHostEnvironment.ContentRootPath,
                        path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
                }

                if (!File.Exists(path))
                {
                    _logger.LogWarning("HTML template file not found: {Path}", path);
                    return;
                }
                var fileNameWithExtension = Path.GetFileNameWithoutExtension(path);
                var secondUnderscoreIndex = fileNameWithExtension.IndexOf('_', fileNameWithExtension.IndexOf('_') + 1);

                var fileNameWithoutPrefix = secondUnderscoreIndex != -1
                    ? fileNameWithExtension.Substring(secondUnderscoreIndex + 1)  // Remove everything before and including the second '_'
                    : fileNameWithExtension;
             

                // Get the file extension
                var ext = Path.GetExtension(path);

                // Combine file name without extension and extension
                dto.HtmlTemplateFileName = $"{fileNameWithoutPrefix}{ext}";                
                dto.HtmlTemplateFileContent = await File.ReadAllBytesAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading HTML template file: {Path}", path);
            }
        }

        /// <summary>
        /// Gets the processed notification content based on TemplateSelection.
        /// If TemplateSelection == "HTMLFILE", reads from HTMLTemplatePath and replaces variables.
        /// If TemplateSelection == "HTMLTEXT" or null, uses EmailContent directly.
        /// </summary>
        public async Task<string?> GetProcessedNotificationContentAsync(NotificationConfig config, Dictionary<string, string> replacements)
        {
            if (config == null)
                return null;

            string content = null;

            // Check if it's an HTML file template
            if (config.TemplateSelection == "HTMLFILE" && !string.IsNullOrWhiteSpace(config.HTMLTemplatePath))
            {
                try
                {
                    var path = config.HTMLTemplatePath.Trim();

                    // Convert to full path if it's relative
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(
                            _webHostEnvironment.ContentRootPath,
                            path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
                    }

                    // Check if file exists
                    if (!File.Exists(path))
                    {
                        _logger.LogWarning("HTML template file not found: {Path}", path);
                        // Fall back to EmailContent if file doesn't exist
                        content = config.EmailContent;
                    }
                    else
                    {
                        // Read file content
                        content = await File.ReadAllTextAsync(path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading HTML template file from path: {Path}", config.HTMLTemplatePath);
                    // Fall back to EmailContent if there's an error
                    content = config.EmailContent;
                }
            }
            else
            {
                // Use EmailContent for HTMLTEXT or when TemplateSelection is not specified
                content = config.EmailContent;
            }

            // Apply replacements to the content
            if (!string.IsNullOrWhiteSpace(content) && replacements != null && replacements.Count > 0)
            {
                foreach (var kvp in replacements)
                {
                    content = content.Replace(kvp.Key, kvp.Value ?? "");
                }
            }

            return content;
        }
    }
}

