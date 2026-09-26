using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeSalaryConfigService
    {
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, string? dropdownFilter =null, DateTime? date = null);
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigsSp(string? searchText = null, string? dropdownFilter = null, bool isLatest = true);
        Task<EmployeeSalaryConfigDto?> GetConfigById(int id);
        Task<LatestApprovedEmployeeSalaryConfigResponseDto?> GetLatestApprovedConfigByEmployeeId(int idEmployee);
        Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> UnApproveEmployeeSalaryConfig(int idEmployeeSalaryConfig, int idEmployee);
        Task<int?> GetNotConfiguredEmployeeCount();
        
    }
}

