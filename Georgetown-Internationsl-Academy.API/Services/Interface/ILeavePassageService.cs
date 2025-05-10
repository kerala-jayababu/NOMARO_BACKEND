using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ILeavePassageService
    {
        Task<IEnumerable<LeavePassageDto>> GetLeavePassagesList( string? searchText ,string? dropdownFilter = null);
        Task<IEnumerable<LeavePassageDto>> GetLeavePassagesListByemployeeId(int EmployeeId);        
        Task<LeavePassageDto?> GetLeavePassagesById(int id);
        Task<LeavePassageDto?> AddLeavePassages(LeavePassageDto leavePassage, int IdEmployee);
        Task<LeavePassageDto?> UpdateLeavePassage(LeavePassageDto leavePassage, int IdEmployee);
    }
}
