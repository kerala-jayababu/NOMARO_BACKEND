using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryTemplateDetailsService
    {
        Task<IEnumerable<SalaryTemplateDetailDto>> GetAllSalaryTemplateDetails();
        Task<SalaryTemplateDetailDto?> GetSalaryTemplateDetailById(int id);
        Task<SalaryTemplateDetailDto?> AddSalaryTemplateDetail(SalaryTemplateDetailDto dto);
        Task<SalaryTemplateDetailDto?> UpdateSalaryTemplateDetail(SalaryTemplateDetailDto dto);
    }
}
