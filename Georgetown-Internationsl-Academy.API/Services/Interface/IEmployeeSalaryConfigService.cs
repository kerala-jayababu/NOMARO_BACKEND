using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeSalaryConfigService
    {
        Task<IEnumerable<EmployeeSalaryConfigDto>> GetAllConfigs(string? searchText = null, DateTime? dateFilter = null,string? dropdownFilter =null);
        Task<EmployeeSalaryConfigDto?> GetConfigById(int id);
        Task<EmployeeSalaryConfigDto?> AddConfig(EmployeeSalaryConfigDto dto, int IdEmployee);
        Task<EmployeeSalaryConfigDto?> UpdateConfig(EmployeeSalaryConfigDto dto);

        Task<IEnumerable<EmployeeSalaryConfigDetailsDto>> GetAllDetails(int Id);
        //Task<EmployeeSalaryConfigDetailsDto?> GetDetailById(int id);
        Task<EmployeeSalaryConfigDetailsDto?> AddDetail(EmployeeSalaryConfigDetailsDto dto);
        Task<EmployeeSalaryConfigDetailsDto?> UpdateDetail(EmployeeSalaryConfigDetailsDto dto);
    }
}
