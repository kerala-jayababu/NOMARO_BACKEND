using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IMaternityLeaveSalaryService
    {
        Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries(string? searchText = null, DateTime? fromDate = null);
        Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int id);
        Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto,int IdEmployee);
        Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto,int EmployeeId);
     
    }
}
