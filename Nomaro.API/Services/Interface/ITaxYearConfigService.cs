using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ITaxYearConfigService
    {
        Task<IEnumerable<TaxYearConfigDto>> GetTaxYearConfigs(int? idFinancialYear = null);
        Task<TaxYearConfigDto?> GetTaxYearConfigById(int id);
        Task<bool> AddOrUpdateTaxYearConfig(TaxYearConfigDto dto);
    }
}
