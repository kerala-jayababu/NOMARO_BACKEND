using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface ICurrencyConversionService
    {
        Task<IEnumerable<CurrencyConversionDto>> GetAllCurrencyConversions();
        Task<CurrencyConversionDto?> GetCurrencyConversionById(int id);
        Task<CurrencyConversionDto?> AddCurrencyConversion(CurrencyConversionDto dto);
        Task<CurrencyConversionDto?> UpdateCurrencyConversion(CurrencyConversionDto dto);
     
        
    }

}

