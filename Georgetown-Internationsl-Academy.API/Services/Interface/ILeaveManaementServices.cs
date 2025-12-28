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

    }
}
