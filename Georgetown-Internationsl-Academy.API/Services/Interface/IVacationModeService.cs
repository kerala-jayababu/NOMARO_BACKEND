using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IVacationModeService
    {
        Task<IEnumerable<VacationModeDto>> GetAllVacationModes();
        Task<VacationModeDto?> GetVacationModeById(int id);
        Task<VacationModeDto?> AddVacationMode(VacationModeDto vacationModeDto);
        Task<VacationModeDto?> UpdateVacationMode(VacationModeDto vacationModeDto);
    }
}
