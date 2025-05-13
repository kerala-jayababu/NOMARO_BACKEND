using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ICurrencyConversionService
    {
        Task<IEnumerable<CurrencyConversionDto>> GetAllCurrencyConversions();
        Task<CurrencyConversionDto?> GetCurrencyConversionById(int id);
        Task<CurrencyConversionDto?> AddCurrencyConversion(CurrencyConversionDto dto);
        Task<CurrencyConversionDto?> UpdateCurrencyConversion(CurrencyConversionDto dto);
     
        
    }

}
