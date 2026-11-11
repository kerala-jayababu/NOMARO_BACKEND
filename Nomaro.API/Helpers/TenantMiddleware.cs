using Nomaro.API.Services.Interface;
using System.Text.RegularExpressions;

namespace Nomaro.API.Helpers
{
    // Resolves the company/tenant from URLs shaped like /api/v1/{companyCode}/{controller}/{action}.
    // When the segment after the API version matches a company code in NOMARO_GLOBAL.dbo.NomaroInstanceConfig,
    // the tenant connection string is stored on the scoped ITenantService and the company segment is stripped
    // from the path so existing controller routes keep working unchanged.
    // When the segment is not a company code (e.g. a controller name), the request passes through untouched
    // and the default connection string is used.
    public class TenantMiddleware
    {
        private static readonly Regex TenantPathPattern = new Regex(
            @"^(?<prefix>/api/v[\d.]+)/(?<companyCode>[^/]+)(?<rest>/.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var match = TenantPathPattern.Match(path);
            IDisposable? countryTimeScope = null;

            if (match.Success)
            {
                var companyCode = match.Groups["companyCode"].Value;
                var config = await tenantService.GetTenantConfigAsync(companyCode);

                if (config != null && !string.IsNullOrWhiteSpace(config.DBConnectionString))
                {
                    tenantService.SetTenant(config.CompanyCode ?? companyCode, config.DBConnectionString);
                    countryTimeScope = CompanyTime.SetTimeZoneId(config.TimeZoneId);

                    var rewrittenPath = match.Groups["prefix"].Value + match.Groups["rest"].Value;
                    context.Request.Path = new PathString(rewrittenPath);

                    _logger.LogDebug("Tenant '{CompanyCode}' resolved from URL. Path rewritten to '{Path}'.", companyCode, rewrittenPath);
                }
            }

            try
            {
                await _next(context);
            }
            finally
            {
                countryTimeScope?.Dispose();
            }
        }
    }
}
