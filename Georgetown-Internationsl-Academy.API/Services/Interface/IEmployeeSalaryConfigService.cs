using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeSalaryConfigService
    {
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs();
        Task<EmployeeSalaryConfigDto?> GetConfigById(int id);
        Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto);
        Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto);

        Task<IEnumerable<EmployeeSalaryConfigDetailsDto>> GetAllDetails();
        Task<EmployeeSalaryConfigDetailsDto?> GetDetailById(int id);
        Task<EmployeeSalaryConfigDetailsDto?> AddDetail(EmployeeSalaryConfigDetailsDto dto);
        Task<EmployeeSalaryConfigDetailsDto?> UpdateDetail(EmployeeSalaryConfigDetailsDto dto);
    }
}
