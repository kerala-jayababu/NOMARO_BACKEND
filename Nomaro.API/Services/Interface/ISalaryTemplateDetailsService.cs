using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryTemplateDetailsService
    {
        Task<IEnumerable<SalaryTemplateDetailDto>> GetAllSalaryTemplateDetails(int Id);
        Task<SalaryTemplateDetailDto?> GetSalaryTemplateDetailById(int id);
        Task<SalaryTemplateDetailDto?> AddSalaryTemplateDetail(SalaryTemplateDetailDto dto);
        Task<SalaryTemplateDetailDto?> UpdateSalaryTemplateDetail(SalaryTemplateDetailDto dto);
    }
}

