using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryAdjustmentService
    {
        Task<IEnumerable<SalaryAdjustmentDto>> GetAllSalaryAdjustments(string? searchText = null, DateTime? fromDate = null);
        Task<SalaryAdjustmentDto?> GetSalaryAdjustmentById(int id);
        Task<SalaryAdjustmentDto?> AddSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment);
        Task<SalaryAdjustmentDto?> UpdateSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment);
    }

}

