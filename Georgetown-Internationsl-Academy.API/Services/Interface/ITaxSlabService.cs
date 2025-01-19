using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ITaxSlabService
    {
        Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabs();
        Task<TaxSlabDto?> GetTaxSlabById(int id);
        Task<TaxSlabDto?> AddTaxSlab(TaxSlabDto taxSlabDto);
        Task<TaxSlabDto?> UpdateTaxSlab(TaxSlabDto taxSlabDto);
    }
}
