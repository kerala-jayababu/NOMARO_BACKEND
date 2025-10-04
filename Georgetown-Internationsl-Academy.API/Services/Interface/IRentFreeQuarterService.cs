using Georgetown_Internationsl_Academy.API.DTO;
using iText.Layout.Properties;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IRentFreeQuarterService
    {
        Task<IEnumerable<RentFreeQuarterDto>> GetRentFreeQuarters(string? searchText = null, DateTime? fromDate = null);
        Task<IEnumerable<RentFreeQuarterDurationsDto>> GetRentFreeQuarterDurations();        
        Task<RentFreeQuarterDto?> GetRentFreeQuarterById(int id);
        Task<RentFreeQuarterDto?> AddRentFreeQuarter(RentFreeQuarterDto dto);
        Task<RentFreeQuarterDto?> UpdateRentFreeQuarter(RentFreeQuarterDto dto);
        Task<IEnumerable<RentFreeQuarterAllowanceDto>> GetRentFreeQuarterAllowanceList(int? financialYear = null, string? searchString = null);
        Task<RentFreeQuarterAllowanceDto?> GetRentFreeQuarterAllowanceById(int id);
        Task<RentFreeQuarterAllowanceAddOrUpdateDto?> AddRentFreeQuarterAllowance(RentFreeQuarterAllowanceAddOrUpdateDto dto,int Idemployee);
        Task<RentFreeQuarterAllowanceAddOrUpdateDto?> UpdateRentFreeQuarterAllowance(RentFreeQuarterAllowanceAddOrUpdateDto dto,int IdEmployee);

    }
}
