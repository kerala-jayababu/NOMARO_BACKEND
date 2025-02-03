using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryHeadServices
    {
        Task<IEnumerable<SalaryHeadDto>> GetSalaryHeadList();
        Task<SalaryHeadDto?> GetSalaryHeadByID(int id);
        Task<SalaryHeadDto?> AddSalaryHead(SalaryHeadDto salaryHead,int IdEmployee);
        Task<SalaryHeadDto?> UpdateSalaryHead(SalaryHeadDto salaryHead,int IdEmployee);        
    }
}
