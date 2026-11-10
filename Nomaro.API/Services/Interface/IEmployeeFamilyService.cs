using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IEmployeeFamilyService
    {
        Task<List<EmployeeFamilyMemberDto>> GetFamilyMembers(int idEmployee);
        Task<int> AddOrUpdateFamilyMember(EmployeeFamilyMemberDto dto, int userId);
        Task DeleteFamilyMember(int idEmployeeFamilyMember, int userId);
        Task UpdateChildrenCount(int idEmployee);
    }
}
