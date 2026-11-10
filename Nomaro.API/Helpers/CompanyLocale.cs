using Nomaro.API.Services.Interface;

namespace Nomaro.API.Helpers
{
    // Static accessor for the current company's locale settings (DateFormat, CurrencySymbol)
    // resolved from the tenant's row in NOMARO_GLOBAL. Falls back to the previous
    // hard-coded defaults (dd/MM/yyyy, ₹) when no tenant is resolved.
    public static class CompanyLocale
    {
        private static IServiceProvider? _serviceProvider;

        // Called once at startup from Program.cs.
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // The company's date format (e.g. "dd/MM/yyyy"), default dd/MM/yyyy.
        public static string DateFormat => Config()?.DateFormat ?? "dd/MM/yyyy";

        // The company's currency symbol (e.g. "AED", "₹"), default ₹.
        public static string CurrencySymbol => Config()?.CurrencySymbol ?? "₹";

        // Formats a date using the company's date format.
        public static string FormatDate(DateTime date)
        {
            var fmt = DateFormat;
            try
            {
                return date.ToString(fmt);
            }
            catch (FormatException)
            {
                return date.ToString("dd/MM/yyyy");
            }
        }

        private static DTO.TenantConfigDto? Config()
        {
            if (_serviceProvider == null) return null;

            var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
            var requestServices = httpContextAccessor?.HttpContext?.RequestServices;
            ITenantService? tenantService;
            if (requestServices != null)
            {
                tenantService = requestServices.GetService<ITenantService>();
            }
            else
            {
                using var scope = _serviceProvider.CreateScope();
                tenantService = scope.ServiceProvider.GetService<ITenantService>();
                return Lookup(tenantService);
            }
            return Lookup(tenantService);
        }

        private static DTO.TenantConfigDto? Lookup(ITenantService? tenantService)
        {
            var companyCode = tenantService?.CompanyCode;
            if (string.IsNullOrWhiteSpace(companyCode) || tenantService == null) return null;
            try
            {
                return tenantService.GetTenantConfigDtoAsync(companyCode).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }
    }
}
