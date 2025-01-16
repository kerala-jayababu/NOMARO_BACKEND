using Georgetown_Internationsl_Academy.API.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IOptionService
    {
        Task<List<SelectOptionIntDto>> GetSalaryHeadsOptions();
        Task<List<SelectOptionIntDto>> GetBudgetCodeOptions();
        Task<List<SelectOptionIntDto>> GetBanksOptions();
        

    }
}
