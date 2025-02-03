using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IMaternityLeaveSalaryService
    {
        Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries();
        Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int id);
        Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto);
        Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto);
        Task<MaternityLeaveSalaryDetailDto?> GetMaternityLeaveSalaryDetailByIdAsync(int id);
        Task<MaternityLeaveSalaryDetailDto> AddMaternityLeaveSalaryDetail(MaternityLeaveSalaryDetailDto entity,int IdEmployee);
        Task<MaternityLeaveSalaryDetailDto> UpdateMaternityLeaveSalaryDetail(MaternityLeaveSalaryDetailDto entity);
    }
}
