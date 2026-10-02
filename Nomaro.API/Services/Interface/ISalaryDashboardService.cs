using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryDashboardService
    {
        Task<SalaryDashboardFilterDto> GetFilterOptions();
        Task<SalaryDashboardDto> GetSalaryDashboard(int idSalaryMonth, int? idOffice, int? idDepartment, bool approvedOnly);
    }
}
