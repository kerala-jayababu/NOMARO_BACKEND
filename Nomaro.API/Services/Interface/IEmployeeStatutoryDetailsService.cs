using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeStatutoryDetailsService
    {
        Task<IEnumerable<EmployeeStatutoryDetailsDto>> GetEmployeeStatutoryDetails(string? searchText);
        Task<EmployeeStatutoryDetailsDto?> GetEmployeeStatutoryDetailsById(int idEmployee);
        Task<bool> IsEmployeeExists(int idEmployee);
        Task<bool> IsStatutoryDetailsExists(int idEmployee);
        Task<bool> AddOrUpdateEmployeeStatutoryDetails(EmployeeStatutoryDetailsDto dto, int idLoggedInEmployee);
        Task<IEnumerable<StateDto>> GetStates(bool? hasPT, bool? hasLWF, bool? isActive);
    }
}
