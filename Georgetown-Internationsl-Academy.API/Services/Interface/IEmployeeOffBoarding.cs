using Georgetown_Internationsl_Academy.API.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IEmployeeOffBoarding
    {

        Task<IEnumerable<ExitReasonDto>> GetExitReasons();
        Task<bool> AddOrUpdateExitReasons(ExitReasonDto dto);

        Task<IEnumerable<ExitTypeDto>> GetExitTypes();
        Task<bool> AddOrUpdateExitTypes(ExitTypeDto dto);

        Task<IEnumerable<NoticePeriodPolicyDto>> GetNoticePeriodPolicies();
        Task<bool> AddOrUpdateNoticePeriodPolicies(NoticePeriodPolicyDto dto);

        Task<IEnumerable<ClearanceTemplateDto>> GetClearanceTemplates();
        Task<bool> AddOrUpdateClearanceTemplates(ClearanceTemplateDto dto);
        Task<IEnumerable<ClearanceTemplateDepartmentDto>> GetClearanceTemplateDepartments(int idClearanceTemplate);
        Task<bool> AddOrUpdateClearanceTemplateDepartment(List<ClearanceTemplateDepartmentDto> dtos);

        // Resignation/Exit Cases
        Task<SubmitResignationResponseDto> SubmitResignation(SubmitResignationDto dto, int loggedInEmployeeId);
        Task<IEnumerable<ResignationRequestDto>> GetResignationRequests(int idLoggedInEmployee, string? roleType = null, int? idEmployee = null, DateTime? initiationDate = null);
        Task<ReportingOfficerActionResponseDto> SubmitReportingOfficerActions(SubmitReportingOfficerActionsDto dto, int loggedInEmployeeId);
        Task<bool> DeleteClearanceTemplateDepartment(int IdTemplateDept);
    }
}
