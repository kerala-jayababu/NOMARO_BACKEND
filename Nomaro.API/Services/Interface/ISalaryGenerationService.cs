using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface ISalaryGenerationService
    {
        Task<IEnumerable<SalaryGenerationDto>> GetSalaryConfigs(int employeeId, int? idSalaryMonth = null, string? dropdownFilter = null,int? idDepartment = null,int? idDesignation = null);
        Task<IEnumerable<SalarySlipDto>> GetSalarySlips(int idSalaryMonthFrom, int idSalaryMonthTo, string? dropdownFilter = null);
        Task<IEnumerable<SalaryGenerationStatusDto>> GenerateSalaryDraft(int idSalaryMonth, int idEmployeeCreated);
        Task<dynamic> GetSalaryapprovalValue(int IdEmployee);        
        Task<dynamic> ExportSalaryGenerationDetails(string employeeIds, int idSalaryMonth);
        Task<dynamic> ExportSalaryGenerationDetailsForApproved(string employeeIds, int idSalaryMonth);
        Task<List<SalaryUploadResponseDto>> UploadSalaryDetails(UploadSalaryGenerationDetailsDto uploadSalaryGenerationDetails,int employeeID);
        Task<bool> SubmitSalaryDetails(string employeeIds, int idSalaryMonth, int idEmployeeCreated);        
        Task<int> UndoGeneratedDraftSalary(string employeeIds, int idSalaryMonth);        
        Task<IEnumerable<SalaryGenerationDetailsDto>> GetSalaryGeneratedDetails(int? idSalaryMonth = null, string? dropdownFilter = null);
        Task<List<EmployeePayslipDto>> GeneratePayslipPdf(string idEmployeeSalary);
        Task<EmployeePayslipDto> GetPayslipObjectAsync(int idEmployeeSalary);
        Task<EmployeePayslipDto> GetPayslipDetailsForLeavePassage(int IdEmployee);
        
        Task<List<EmployeePayslipDto>> GenerateNotificationForEmployeeSalary(string? idEmployeeSalary);
        Task MarkSalaryEmailInProcessAsync(int idEmployeeSalary);
        Task MarkSalaryEmailSentAsync(int idEmployeeSalary);
        Task<bool> CheckcurrencyConversions();

        // EmployeesForSalaryGenerationStatus
        Task<IEnumerable<EmployeesForSalaryGenerationStatusDto>> GetEmployeesForSalaryGenerationStatus(int? idSalaryMonth = null);
        Task<EmployeesForSalaryGenerationStatusDto?> GetEmployeeForSalaryGenerationStatus(int idEmployee, int idSalaryMonth);
        Task<int> AddEmployeesForSalaryGenerationStatus(List<EmployeesForSalaryGenerationStatusDto> dtos);
        Task<int> UpdateEmployeesForSalaryGenerationStatus(List<EmployeesForSalaryGenerationStatusDto> dtos);
        Task<bool> DeleteEmployeeForSalaryGenerationStatus(int idEmployee, int idSalaryMonth);
        Task<int> DeleteAllEmployeesForSalaryGenerationStatus(int? idSalaryMonth = null);



    }
}

