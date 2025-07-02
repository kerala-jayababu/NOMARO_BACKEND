using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeSalaryConfigService
    {
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, string? dropdownFilter =null, bool showLatestRecord = false);
        Task<EmployeeSalaryConfigDto?> GetConfigById(int id);
        Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        
    }
}
