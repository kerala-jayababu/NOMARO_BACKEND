using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IChildTaxThresholdService
    {
        Task<IEnumerable<ChildTaxThresholdDto>> GetAllChildTaxThresholds(int? idFinancialYear = null);
        Task<ChildTaxThresholdDto?> GetChildTaxThresholdById(int id);
        Task<ChildTaxThresholdDto?> AddChildTaxThreshold(ChildTaxThresholdDto dto);
        Task<ChildTaxThresholdDto?> UpdateChildTaxThreshold(ChildTaxThresholdDto dto);
    }
}

