using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IMaternityLeaveSalaryService
    {
        Task<IEnumerable<MaternityLeaveSalaryDto>> GetAllMaternityLeaveSalaries();
        Task<MaternityLeaveSalaryDto?> GetMaternityLeaveSalaryById(int id);
        Task<MaternityLeaveSalaryDto?> AddMaternityLeaveSalary(MaternityLeaveSalaryDto dto);
        Task<MaternityLeaveSalaryDto?> UpdateMaternityLeaveSalary(MaternityLeaveSalaryDto dto);
    }
}
