using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface ILeavePassageService
    {
        Task<IEnumerable<LeavePassageDto>> GetLeavePassagesList( string? searchText ,string? dropdownFilter = null);
        Task<IEnumerable<LeavePassageDto>> GetLeavePassagesListByemployeeId(int EmployeeId);        
        Task<LeavePassageDto?> GetLeavePassagesById(int id);
        Task<LeavePassageDto?> AddLeavePassages(LeavePassageDto leavePassage, int IdEmployee);
        Task<LeavePassageDto?> UpdateLeavePassage(LeavePassageDto leavePassage, int IdEmployee);
        Task<IEnumerable<LeavePassageAmountDto>> GetLeavePassageAmountDetails(int? financialYear = null, string? searchString = null);
        Task<bool> SubmitLeavePassageAsync(List<LeavePassageAmountDetailsDto> leavePassages, int LoginedIdEmployee);
        Task<bool> SubmitLeavePassageReversalAsync(List<LeavePassageReversalDto> leavePassageReversal, int LoginedIdEmployee);
        Task<bool> SubmitLeavePassageAdditionAsync(List<LeavePassageAdditionDto> leavePassageReversal, int LoginedIdEmployee);
        Task<List<WorkMonthsInYearDto>> GetCurrentWorkYearMonths();
        Task<IEnumerable<LeavePassageForHRDto>> GetLeavePassageRequestsForHR(
          int idWorkYear,string requestStatus,string? searchText,string? approvalStatus = null);
    }
}

