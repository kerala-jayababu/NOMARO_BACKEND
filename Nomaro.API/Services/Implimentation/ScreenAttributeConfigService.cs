using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    public class ScreenAttributeConfigService : IScreenAttributeConfigService
    {
        private readonly GlobalDbContext _globalDbContext;
        private readonly ILogger<ScreenAttributeConfigService> _logger;

        public ScreenAttributeConfigService(
            GlobalDbContext globalDbContext,
            ILogger<ScreenAttributeConfigService> logger)
        {
            _globalDbContext = globalDbContext;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ScreenAttributeConfigDto>> GetByScreenCodeAsync(
            string companyCode,
            string screenCode,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                throw new ArgumentException("A company code is required to load screen field configuration.", nameof(companyCode));
            }

            if (string.IsNullOrWhiteSpace(screenCode))
            {
                throw new ArgumentException("A screen code is required to load screen field configuration.", nameof(screenCode));
            }

            try
            {
                return await _globalDbContext.CompanyScreenAttributeConfigs
                    .AsNoTracking()
                    .Where(config => config.CompanyCode == companyCode && config.ScreenCode == screenCode)
                    .OrderBy(config => config.AttributeId)
                    .Select(config => new ScreenAttributeConfigDto
                    {
                        AttributeId = config.AttributeId,
                        DisplayLabel = config.DisplayLabel,
                        FieldVisibility = config.FieldVisibility,
                        MaxLength = config.MaxLength,
                        DataType = config.DataType
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load screen attribute configuration for company {CompanyCode} and screen {ScreenCode}.",
                    companyCode,
                    screenCode);
                throw;
            }
        }
    }
}
