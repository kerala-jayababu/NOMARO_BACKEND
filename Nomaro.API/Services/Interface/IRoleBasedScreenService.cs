using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface IRoleBasedScreenService
    {
        Task<bool> ManagePayrollScreens(List<PayrollScreenDto> payrollScreens);
        Task<List<PayrollScreenDto>> GetAllPayrollScreens(string? appType,int IdEmployee);
        Task<List<PayrollScreenDto>> GetEmployeePermissionsById(int EmployeeID);
        Task<bool> ManageEmployeePermissions(List<EmployeePermissionDto> employeePermissions);
        Task<bool> ManageRoleBasedPermissions(List<RoleBasedPermissionDto> rolePermissions);
        Task<List<RoleBasedScreenDto>> GetRoleBasedPermissionsByDesignationId(int designationId);
        Task<EmployeePermissions> GetEmployeePermission(int EmployeeID,int PayrollScreen);
        Task<bool>CheckEmployeePermission(int employeeId, string screenCode, string actionType);
    }
}


