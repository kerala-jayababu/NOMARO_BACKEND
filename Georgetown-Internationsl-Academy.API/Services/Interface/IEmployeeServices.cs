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
    }
}
