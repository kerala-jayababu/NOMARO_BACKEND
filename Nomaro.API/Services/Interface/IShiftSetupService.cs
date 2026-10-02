using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IShiftSetupService
    {
        Task<IEnumerable<ShiftSetupOfficeDto>> GetShiftSetupDetails(int employeeId);
        Task<IEnumerable<ShiftSetupOfficeDto>> GetShiftSetupHierarchyByOffice(int employeeId, int officeId);
        Task<IEnumerable<ShiftManagerEmployeeDto>> GetShiftManagerEmployees(int employeeId, int officeId);
        Task UpdateShiftManagerAssignments(int employeeId, List<ShiftManagerAssignmentDto> assignments);
    }
}