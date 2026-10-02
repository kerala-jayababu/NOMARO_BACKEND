using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ITaxSlabService
    {
        Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabs(int idTaxYearConfig, string? ageCategory = null);
        Task<TaxSlabDto?> GetTaxSlabById(int id);
        Task<TaxSlabDto?> AddTaxSlab(TaxSlabDto taxSlabDto);
        Task<TaxSlabDto?> UpdateTaxSlab(TaxSlabDto taxSlabDto);
    }
}
