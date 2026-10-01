using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryHeadServices
    {
        Task<IEnumerable<SalaryHeadDto>> GetSalaryHeadList();
        Task<SalaryHeadDto?> GetSalaryHeadByID(int id);
        Task<SalaryHeadDto?> AddSalaryHead(SalaryHeadDto salaryHead,int IdEmployee);
        Task<SalaryHeadDto?> UpdateSalaryHead(SalaryHeadDto salaryHead,int IdEmployee);
        Task<List<string>> GetSalaryHeadUsage(int idSalaryHead);        
      
    }
}

