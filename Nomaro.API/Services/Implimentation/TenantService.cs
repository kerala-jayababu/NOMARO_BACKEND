using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Nomaro.API.Services.Implementation
{
    public class TenantService : ITenantService
    {
        private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MissingCacheDuration = TimeSpan.FromMinutes(5);

        private readonly GlobalDbContext _globalDbContext;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantService> _logger;

        public string? CompanyCode { get; private set; }
        private string? _connectionString;

        public TenantService(GlobalDbContext globalDbContext, IMemoryCache cache, IConfiguration configuration, ILogger<TenantService> logger)
        {
            _globalDbContext = globalDbContext;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public void SetTenant(string companyCode, string connectionString)
        {
            CompanyCode = companyCode;
            _connectionString = connectionString;
        }

        public string ResolveConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                return _connectionString;
            }
            return _configuration.GetConnectionString("DbContext") ?? string.Empty;
        }

        public async Task<NomaroInstanceConfig?> GetTenantConfigAsync(string companyCode)
        {
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                return null;
            }

            var cacheKey = $"tenant_config_{companyCode.Trim().ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out NomaroInstanceConfig? cached))
            {
                return cached;
            }

            var config = await _globalDbContext.NomaroInstanceConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyCode != null && c.CompanyCode.ToUpper() == companyCode.Trim().ToUpper());

            // Cache hits and misses so controller-name segments (e.g. "Account") don't hit the global DB per request.
            var duration = config != null ? ConfigCacheDuration : MissingCacheDuration;
            _cache.Set(cacheKey, config, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration });

            return config;
        }

        public async Task<TenantConfigDto?> GetTenantConfigDtoAsync(string companyCode)
        {
            var config = await GetTenantConfigAsync(companyCode);
            if (config == null)
            {
                return null;
            }

            return new TenantConfigDto
            {
                CountryCode = config.CountryCode,
                CountryName = config.CountryName,
                CompanyCode = config.CompanyCode,
                CompanyName = config.CompanyName,
                DatabaseName = ExtractDatabaseName(config.DBConnectionString),
                DateFormat = config.DateFormat,
                CurrencySymbol = config.CurrencySymbol,
                CurrencyName = config.CurrencyName,
                CurrencyCode = config.CurrencyCode
            };
        }

        private static string? ExtractDatabaseName(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2 &&
                    (keyValue[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase) ||
                     keyValue[0].Trim().Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)))
                {
                    return keyValue[1].Trim();
                }
            }
            return null;
        }
    }
}
