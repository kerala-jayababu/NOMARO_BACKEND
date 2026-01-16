using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeOffBoarding
    {

        Task<IEnumerable<ExitReasonDto>> GetExitReasons();
        Task<bool> AddOrUpdateExitReasons(List<ExitReasonDto> dtos);

        Task<IEnumerable<ExitTypeDto>> GetExitTypes();
        Task<bool> AddOrUpdateExitTypes(List<ExitTypeDto> dtos);

        Task<IEnumerable<NoticePeriodPolicyDto>> GetNoticePeriodPolicies();
        Task<bool> AddOrUpdateNoticePeriodPolicies(List<NoticePeriodPolicyDto> dtos);

        Task<IEnumerable<ClearanceTemplateDto>> GetClearanceTemplates();
        Task<bool> AddOrUpdateClearanceTemplates(List<ClearanceTemplateDto> dtos);
    }
}
