using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryAdjustmentService
    {
        Task<IEnumerable<SalaryAdjustmentDto>> GetAllSalaryAdjustments();
        Task<SalaryAdjustmentDto?> GetSalaryAdjustmentById(int id);
        Task<SalaryAdjustmentDto?> AddSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment);
        Task<SalaryAdjustmentDto?> UpdateSalaryAdjustment(SalaryAdjustmentDto salaryAdjustment);
    }

}
