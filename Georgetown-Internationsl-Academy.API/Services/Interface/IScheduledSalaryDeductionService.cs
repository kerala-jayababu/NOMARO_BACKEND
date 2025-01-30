using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IScheduledSalaryDeductionService
    {
        Task<IEnumerable<ScheduledSalaryDeductionDto>> GetScheduledDeductions();
        Task<ScheduledSalaryDeductionDto?> GetScheduledDeductionById(int id);
        Task<ScheduledSalaryDeductionDto?> AddScheduledDeduction(ScheduledSalaryDeductionDto dto);
        Task<ScheduledSalaryDeductionDto?> UpdateScheduledDeduction(ScheduledSalaryDeductionDto dto);
    }
}
