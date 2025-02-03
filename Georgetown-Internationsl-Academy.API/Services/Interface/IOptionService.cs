using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IOptionService
    {
        Task<AllOptionsDto> GetAllOptions();
        Task<List<SalaryMonthsDto>> GetAllSalaryMonths();
    }
}
