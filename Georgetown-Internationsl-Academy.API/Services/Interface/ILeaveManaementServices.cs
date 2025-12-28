using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ILeaveManaementServices
    {
        Task<IEnumerable<LeaveTypesDto>> GetLeaveTypes();
        Task<bool> AddOrUpdateLeaveTypes(List<LeaveTypesDto> leaveTypeDtos);
        Task<IEnumerable<AnnualLeaveTypeConfigDto>> GetAnnualLeaveTypeConfigs(int? idAnnualLeaveTypeConfig = null,int? idLeaveType = null,int? idYear = null,bool? isActive = null);
        Task<List<int>> AddUpdateAnnualLeaveTypeConfig(List<AnnualLeaveTypeConfigDto> configDtoList, int loggedInEmployeeId);
        Task<bool> DeactivateAnnualLeaveTypeConfig(int idAnnualLeaveTypeConfig, int loggedInEmployeeId);
        Task<IEnumerable<LeaveTemplateDto>> GetLeaveTemplates(int? idLeaveTemplate = null,bool? isActive = null,string? searchText = null);
        Task<LeaveTemplateWithDetailsDto> GetLeaveTemplate(int idLeaveTemplate);
        Task<LeaveTemplateSaveResponseDto> AddUpdateLeaveTemplate(LeaveTemplatePostDto dto, int loggedInEmployeeId);
        Task<bool> DeactivateLeaveTemplate(int idLeaveTemplate, int loggedInEmployeeId);
        Task<EmployeeLeaveSetupDto> GetEmployeeLeaveSetup(int idEmployee, DateTime? activeOnDate = null);
        Task<int> AddUpdateEmployeeLeaveConfig(EmployeeLeaveConfigPostDto dto, int loggedInEmployeeId);
        Task<PagedResultDto<LeaveApplicationListDto>> GetLeaveApplications(
            int loggedInEmployeeId,
            int? idEmployee,
            string? approvalStatus,
            string? applicationStatus,
            DateTime? fromDate,
            DateTime? toDate,
            int? idLeaveType,
            PagingRequestDto paging);

        Task<LeaveApplicationDetailsDto> GetLeaveApplication(int idLeaveApplication);
        Task<LeaveApplicationSaveResultDto> AddUpdateLeaveApplication(LeaveApplicationPostDto dto, int loggedInEmployeeId);
        Task<bool> DeleteLeaveApplicationDocument(int idLeaveApplicationDocument, int loggedInEmployeeId, bool isHrOverride);
        Task<CancelLeaveApplicationResultDto> CancelLeaveApplication(int idLeaveApplication, string? cancelReason, int loggedInEmployeeId);

    }
}
