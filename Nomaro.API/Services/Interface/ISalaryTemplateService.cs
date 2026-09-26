using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryTemplateService
    {
        Task<IEnumerable<SalaryTemplateDto>> GetAllSalaryTemplates(string? searchText = null,  string? dropdownFilter = null);
        Task<SalaryTemplateDto?> GetSalaryTemplateById(int id);
        Task<SalaryTemplateDto?> AddSalaryTemplate(SalaryTemplateDto salaryTemplate, int IdEmployee);
        Task<SalaryTemplateDto?> UpdateSalaryTemplate(SalaryTemplateDto salaryTemplate, int IdEmployee);
    }
}

