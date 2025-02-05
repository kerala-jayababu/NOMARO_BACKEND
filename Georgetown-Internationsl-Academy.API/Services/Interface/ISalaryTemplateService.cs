using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryTemplateService
    {
        Task<IEnumerable<SalaryTemplateDto>> GetAllSalaryTemplates(string? searchText = null, DateTime? dateFilter = null, string? dropdownFilter = null);
        Task<SalaryTemplateDto?> GetSalaryTemplateById(int id);
        Task<SalaryTemplateDto?> AddSalaryTemplate(SalaryTemplateManageDto salaryTemplate, int IdEmployee);
        Task<SalaryTemplateDto?> UpdateSalaryTemplate(SalaryTemplateDto salaryTemplate, int IdEmployee);
    }
}
