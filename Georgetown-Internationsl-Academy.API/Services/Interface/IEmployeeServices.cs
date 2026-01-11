using Georgetown_Internationsl_Academy.API.DTO;
using System.ComponentModel;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeServices
    {
        Task<IEnumerable<EmployeeProfileDto>> GetEmployeeList(string? searchText, DateTime? startDate);
        Task<IEnumerable<EmployeeWithoutSalaryApprovalDto>> GetEmployeeStatusListAsync();
        Task<EmployeeDetailsDto> GetEmployeeDetailsByID(int Id);
        Task<IEnumerable<EmployeeBankAccountDto>> GetEmployeeBankAccountsByID(int Id);
        Task<IEnumerable<EmployeeProfileDetailsDto>> GetEmployeeProfileByID(int Id);        
        Task<IEnumerable<EmployeeOvertimeConfigDto>> GetEmployeeOvertimeConfigsByID(int Id);
        Task<bool> UpdateEmployeeDetails(UpdateEmployeeDto updateEmployee);
        Task<bool> DeleteEmployeeAttachment(int  employeeID);        
        Task<bool> ManageEmployeeBankAccounts(List<EmployeeBankAccountDtoList> bankAccounts);
        Task<bool> ManageEmployeeOvertimeConfigs(List<EmployeeOvertimeConfigDtoList> overtimeConfigs);
        Task<IEnumerable<EmployeeHierarchyDto>> GetEmployeesByHierarchy(int employeeId);
        Task<IEnumerable<PayslipDetailsDto>> GetPayslipDetails(int IdEmployee, int IdSalaryMonth);
        Task<IEnumerable<SalaryDetailsEmployeeDto>> GetSalaryDetailsEmployee(int IdEmployee, int IdSalaryMonthFrom, int IdSalaryMonthTo);
        Task<OTPDto> SetOTP(string EmailID);
        Task<OTPStatusDto>  ValidateOTP(string EmailID, string OTP);
        Task<EmployeeEntityDto?> AddEmployee(EmployeeEntityDto dto);
        Task<EmployeeEntityDto?> UpdateEmployee(int id, EmployeeEntityDto dto);
        Task<EmployeeEntityDto?> GetEmployeeById(int id);
        Task<IEnumerable<AssetAssignmentFullDto>> GetAssetAssignments(int? idEmployee, int? idAsset);
        Task<bool> AssignAsset(AssetAssignmentDto dto);
        Task<bool> UnassignAsset(int idAsset, int idEmployee);

        Task<IEnumerable<EmployeeQualificationDto>> GetEmployeeQualifications(int idEmployee, int? idEmployeeQualification = null);

        Task<bool> AddOrUpdateEmployeeQualifications(List<EmployeeQualificationDto> dtos, int loggedInEmployeeId);

        Task<bool> DeleteEmployeeQualification(int idEmployeeQualification);
        Task<IEnumerable<EmployeeExperienceDto>> GetEmployeeExperiences(int idEmployee);
        Task<bool> AddOrUpdateEmployeeExperiences(List<EmployeeExperienceDto> dtos, int loggedInEmployeeId);
        Task<bool> DeleteEmployeeExperience(int idEmployeeExperience);
        Task<IEnumerable<EmployeeActionDto>> GetEmployeeActions(string? searchText = null,string? actionType = null,DateTime dateFrom = default);
        Task<List<int>> PostEmployeeActions(List<EmployeeActionPostDto> dtoList, int loggedInEmployeeId);
        Task<IEnumerable<EmployeeExperienceGetDto>> GetEmployeeExperiences(int idEmployee, int? idEmployeeExperience = null);
        Task<IEnumerable<EmployeeDocumentDto>> GetEmployeeDocuments(int idEmployee, int? idEmployeeDocument = null);
        Task<bool> PostEmployeeDocuments(List<EmployeeDocumentPostDto> dtos, int loggedInEmployeeId);

        Task<IEnumerable<EmployeeServiceChangeListDto>> GetEmployeeServiceChanges(DateTime DateFrom, string? ChangeType, int IdEmployee);
        Task<IEnumerable<EmployeeServiceChangeListForApprovalDto>> GetEmployeeServiceChangesForApproval(
            string Status, string ChangeType, DateTime DateFrom, string? SearchText);
        Task<bool> AddUpdateEmployeeServiceChange(EmployeeServiceChangeDto dto, int loggedInEmployeeId);
        Task<bool> AddUpdateEmployeeServiceChanges(List<EmployeeServiceChangeDto> dtos, int loggedInEmployeeId);

        Task<bool> DeleteEmployeeServiceChanges(int idEmployeeServiceChange, int loggedInEmployeeId, bool isHrManager);
    }
}
