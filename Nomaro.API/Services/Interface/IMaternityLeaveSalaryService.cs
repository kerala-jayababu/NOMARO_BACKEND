using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface IMaternityLeaveSalaryService
    {
        Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries(string? searchText = null, DateTime? fromDate = null);
        Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int id);
        Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto,int IdEmployee);
        Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto,int EmployeeId);
     
    }
}

