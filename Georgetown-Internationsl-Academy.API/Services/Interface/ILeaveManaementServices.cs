using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ILeaveManaementServices
    {
        Task<IEnumerable<LeaveTypesDto>> GetLeaveTypes();
        Task<bool> AddOrUpdateLeaveTypes(List<LeaveTypesDto> leaveTypeDtos);

        Task<IEnumerable<AnnualLeaveTypeConfigDto>> GetAnnualLeaveTypeConfigs(int idYear);
        Task<bool> AddOrUpdateAnnualLeaveTypeConfigs(List<AnnualLeaveTypeConfigDto> configDtos);
    }
}
