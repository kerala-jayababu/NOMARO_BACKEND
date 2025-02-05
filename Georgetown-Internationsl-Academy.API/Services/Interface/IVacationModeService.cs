using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IVacationModeService
    {
        Task<IEnumerable<VacationModeDto>> GetAllVacationModes(string? searchText = null, DateTime? dateFilter = null);
        Task<VacationModeDto?> GetVacationModeById(int id);
        Task<VacationModeDto?> AddVacationMode(VacationModeDto vacationModeDto);
        Task<VacationModeDto?> UpdateVacationMode(VacationModeDto vacationModeDto);
    }
}
