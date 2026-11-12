using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface ITenantService
    {
        // Company code resolved for the current request.
        string? CompanyCode { get; }

        // Marks the current request scope with the tenant resolved from the URL.
        void SetTenant(string companyCode, string connectionString);

        // Connection string for the current request's resolved tenant.
        string ResolveConnectionString();

        // Looks up the tenant row in NOMARO_GLOBAL.dbo.NomaroInstanceConfig (cached).
        // Returns null when no company matches the code.
        Task<NomaroInstanceConfig?> GetTenantConfigAsync(string companyCode);

        // Public-safe view of the tenant configuration (no connection string).
        Task<TenantConfigDto?> GetTenantConfigDtoAsync(string companyCode);
    }
}
