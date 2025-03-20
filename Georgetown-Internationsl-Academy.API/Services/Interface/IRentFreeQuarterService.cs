using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IRentFreeQuarterService
    {
        Task<IEnumerable<RentFreeQuarterDto>> GetRentFreeQuarters(string? searchText = null, DateTime? fromDate = null);
        Task<IEnumerable<RentFreeQuarterDurationsDto>> GetRentFreeQuarterDurations();        
        Task<RentFreeQuarterDto?> GetRentFreeQuarterById(int id);
        Task<RentFreeQuarterDto?> AddRentFreeQuarter(RentFreeQuarterDto dto);
        Task<RentFreeQuarterDto?> UpdateRentFreeQuarter(RentFreeQuarterDto dto);
    }
}
