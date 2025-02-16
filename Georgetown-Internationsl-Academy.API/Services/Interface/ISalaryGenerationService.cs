using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryGenerationService
    {
        Task<IEnumerable<SalaryGenerationDto>> GetSalaryConfigs(int? idSalaryMonth = null,string? dropdownFilter = null,int? idDepartment = null,int? idDesignation = null);
        Task<IEnumerable<SalaryGenerationStatusDto>> GenerateSalaryDraft(int[] employeeIds, int idSalaryMonth, int idEmployeeCreated);

        Task<int> UndoGeneratedDraftSalary(int[] employeeIds, int idSalaryMonth);
    }
}
