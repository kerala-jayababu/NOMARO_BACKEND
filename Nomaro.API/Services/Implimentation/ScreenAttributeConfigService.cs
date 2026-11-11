using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    public class ScreenAttributeConfigService : IScreenAttributeConfigService
    {
        private readonly GlobalDbContext _globalDbContext;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ScreenAttributeConfigService> _logger;

        public ScreenAttributeConfigService(
            GlobalDbContext globalDbContext,
            ITenantService tenantService,
            ILogger<ScreenAttributeConfigService> logger)
        {
            _globalDbContext = globalDbContext;
            _tenantService = tenantService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ScreenAttributeConfigDto>> GetByScreenCodeAsync(
            string screenCode,
            CancellationToken cancellationToken)
        {
            var companyCode = _tenantService.CompanyCode;
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                throw new InvalidOperationException("A company code is required to load screen field configuration.");
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
