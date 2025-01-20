using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IChildTaxThresholdService
    {
        Task<IEnumerable<ChildTaxThresholdDto>> GetAllChildTaxThresholds();
        Task<ChildTaxThresholdDto?> GetChildTaxThresholdById(int id);
        Task<ChildTaxThresholdDto?> AddChildTaxThreshold(ChildTaxThresholdDto dto);
        Task<ChildTaxThresholdDto?> UpdateChildTaxThreshold(ChildTaxThresholdDto dto);
    }
}
