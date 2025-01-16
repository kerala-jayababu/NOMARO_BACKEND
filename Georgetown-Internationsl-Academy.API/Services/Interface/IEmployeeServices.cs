using Georgetown_Internationsl_Academy.API.DTO;
using System.ComponentModel;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeServices
    {
        Task<IEnumerable<EmployeeProfileDto>> GetEmployeeList();
        Task<EmployeeDetailsDto> GetEmployeeDetailsByID(int Id);
        Task<IEnumerable<EmployeeBankAccountDto>> GetEmployeeBankAccountsByID(int Id);
        Task<IEnumerable<EmployeeOvertimeConfigDto>> GetEmployeeOvertimeConfigsByID(int Id);
        


    }
}
