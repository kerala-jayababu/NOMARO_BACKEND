using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ISalaryGenerationService
    {
        Task<IEnumerable<SalaryGenerationDto>> GetSalaryConfigs(int? idSalaryMonth = null,string? dropdownFilter = null,int? idDepartment = null,int? idDesignation = null);
        Task<IEnumerable<SalaryGenerationStatusDto>> GenerateSalaryDraft(string employeeIds, int idSalaryMonth, int idEmployeeCreated);
        Task<dynamic> ExportSalaryGenerationDetails(string employeeIds, int idSalaryMonth);

        Task<List<SalaryUploadResponseDto>> UploadSalaryDetails(UploadSalaryGenerationDetailsDto uploadSalaryGenerationDetails,int employeeID);

        Task<bool> SubmitSalaryDetails(string employeeIds, int idSalaryMonth, int idEmployeeCreated);        
        Task<int> UndoGeneratedDraftSalary(string employeeIds, int idSalaryMonth);        
        Task<IEnumerable<SalaryGenerationDetailsDto>> GetSalaryGeneratedDetails(int? idSalaryMonth = null, string? dropdownFilter = null);
    }
}
