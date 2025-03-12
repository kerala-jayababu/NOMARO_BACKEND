using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IRoleBasedScreenService
    {
        Task<bool> ManagePayrollScreens(List<PayrollScreenDto> payrollScreens);
        Task<List<PayrollScreenDto>> GetAllPayrollScreens(int IdEmployee);
        Task<List<PayrollScreenDto>> GetEmployeePermissionsById(int EmployeeID);
        Task<bool> ManageEmployeePermissions(List<EmployeePermissionDto> employeePermissions);
        Task<bool> ManageRoleBasedPermissions(List<RoleBasedPermissionDto> rolePermissions);
        Task<List<RoleBasedScreenDto>> GetRoleBasedPermissionsByDesignationId(int designationId);
        Task<EmployeePermissions> GetEmployeePermission(int EmployeeID,int PayrollScreen);
    }
}

