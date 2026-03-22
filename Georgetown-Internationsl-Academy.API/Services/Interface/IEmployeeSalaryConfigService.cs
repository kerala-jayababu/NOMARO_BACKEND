using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeSalaryConfigService
    {
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, string? dropdownFilter =null, DateTime? date = null);
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigsSp(string? searchText = null, string? dropdownFilter = null, bool isLatest = true);
        Task<EmployeeSalaryConfigDto?> GetConfigById(int id);
        Task<LatestApprovedEmployeeSalaryConfigResponseDto?> GetLatestApprovedConfigByEmployeeId(int idEmployee);
        Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> SubmitForApprovalAsync(int idEmployeeSalaryConfig, int idEmployee);
        Task<int?> GetNotConfiguredEmployeeCount();
        
    }
}
