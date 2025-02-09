using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IScheduledSalaryDeductionService
    {
        Task<IEnumerable<ScheduledSalaryDeductionDto>> GetScheduledDeductions(string? searchText = null, DateTime? fromDate = null);
        Task<ScheduledSalaryDeductionDto?> GetScheduledDeductionById(int id);
        Task<ScheduledSalaryDeductionDto?> AddScheduledDeduction(ScheduledSalaryDeductionDto dto,int EmployeeId);
        Task<ScheduledSalaryDeductionDto?> UpdateScheduledDeduction(ScheduledSalaryDeductionDto dto, int EmployeeId);
    }
}
