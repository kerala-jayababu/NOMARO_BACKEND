using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IDepartmentServices
    {
        Task<IEnumerable<DepartmentDto>> GetDepartmentList();
        Task<DepartmentDto?> GetDepartmentByID(int id);
        Task<DepartmentDto?> AddDepartment(DepartmentDto department);
        Task<DepartmentDto?> UpdateDepartment(DepartmentDto department);
    }

}
