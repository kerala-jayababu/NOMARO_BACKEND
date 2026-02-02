using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ILeaveManaementServices
    {
        Task<IEnumerable<LeaveTypesDto>> GetLeaveTypes();
        Task<bool> AddOrUpdateLeaveTypes(LeaveTypesDto leaveTypeDto);
        Task<IEnumerable<LeaveTemplateDto>> GetLeaveTemplates(int? IdYear, string Status, string? searchText = null);
        Task<LeaveTemplateDto> GetLeaveTemplateByID(int idLeaveTemplate);
        Task<bool> AddUpdateLeaveTemplate(LeaveTemplatePostDto dto, int loggedInEmployeeId);

        Task<bool> SubmitLeaveTemplateForApproval(int idLeaveTemplate, int loggedInEmployeeId);

        Task<bool> AddOrUpdateLeaveTemplateDetails(LeaveTemplateDetailsDto dto, int loggedInEmployeeId);
        Task<LeaveTemplateDetailsDto> GetLeaveTemplateDetailById(int idLeaveTemplateDetails);

        Task<List<EmployeeLeaveSetupDto>> GetEmployeesLeaveSetup(string? searchText, int? IdYear);
        Task<EmployeeLeaveSetupDto> GetLeaveSetupOfAnEmployee(int idEmployee, int? idYear, DateTime? dateTo);
        Task<bool> AddUpdateEmployeeLeaveConfig(EmployeeLeaveConfigsPostDto dto, int loggedInEmployeeId);
        Task<bool> AddUpdateEmployeeLeaveConfigDetails(EmployeeLeaveConfigDetailsPostDto dto, int loggedInEmployeeId);
        Task<PagedResultDto<LeaveApplicationListDto>> GetLeaveApplications(
            int loggedInEmployeeId,
            int? idEmployee,
            string? approvalStatus,
            string? applicationStatus,
            DateTime? fromDate,
            DateTime? toDate,
            int? idLeaveType,
            string? SearchText,
            PagingRequestDto paging);

        Task<LeaveApplicationDetailsDto> GetLeaveApplication(int idLeaveApplication);
        Task<LeaveApplicationSaveResultDto> AddUpdateLeaveApplication(LeaveApplicationPostDto dto, int loggedInEmployeeId);
        Task<bool> DeleteLeaveApplicationDocument(int idLeaveApplicationDocument, int loggedInEmployeeId, bool isHrOverride);
        Task<CancelLeaveApplicationResultDto> CancelLeaveApplication(int idLeaveApplication, string? cancelReason, int loggedInEmployeeId);
        Task<IEnumerable<LeaveApplicationListDto>> GetLeaveApplicationsForApproval(int loggedInEmployeeId, string? approvalStatus, string? SearchText, DateTime? fromDate);
        Task<List<LeaveDashboardDto>> GetLeaveDashboardEmployee(int idEmployee, int idYear);
        Task<List<LeaveApplicationListDto>> GetLeaveApplicationsEmployee(int idEmployee, DateTime DateFrom, DateTime DateTo);
        Task<List<MonthlyLeaveDashboardDto>> GetLeaveDashboardEmployeeMonthWise(int idEmployee, int idYear);
        Task<decimal> CalculateLeaveDaysAsync(bool IncludeHoliday, DateTime fromDate, DateTime toDate, bool isHalfDay);
        Task<bool> SubmitLeaveApplicationApproval(List<int> idChanges, string approvalStatus, string? remarks, int loggedInEmployeeId);

    }
}
