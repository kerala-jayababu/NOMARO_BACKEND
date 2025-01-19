using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ITaxConfigService
    {
        Task<IEnumerable<TaxConfigDto>> GetAllTaxConfigs();
        Task<TaxConfigDto?> GetTaxConfigById(int id);
        Task<TaxConfigManageDto?> AddTaxConfig(TaxConfigManageDto taxConfigDto);
        Task<TaxConfigManageDto?> UpdateTaxConfig(TaxConfigManageDto taxConfigDto);
    }

}
