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
        Task<bool> DeleteClearanceTemplateDepartment(int IdTemplateDept);

        // Resignation/Exit Cases
        Task<SubmitResignationResponseDto> SubmitResignation(SubmitResignationDto dto, int loggedInEmployeeId);
        Task<IEnumerable<ResignationRequestDto>> GetResignationRequests(int idLoggedInEmployee, string? roleType = null, int? idEmployee = null, DateTime? initiationDate = null);
        Task<ReportingOfficerActionResponseDto> SubmitReportingOfficerActions(SubmitReportingOfficerActionsDto dto, int loggedInEmployeeId); 
        Task<HROfficerActionResponseDto> SubmitHROfficerActions(SubmitHROfficerActionsDto dto,int loggedInEmployeeId);
        Task<HRManagerActionResponseDto> SubmitHRManagerActions(SubmitHRManagerActionsDto dto,int loggedInHRManagerId);
       Task<GetExitClearanceDetailsResponseDto> GetExitClearanceDetails(int loggedInEmployeeId,int idExitCase,int? idDepartment = null,string? viewAsRole = null);
        Task<SubmitExitCaseDepartmentClearanceLinesResponseDto> SubmitExitCaseDepartmentClearanceLines(SubmitExitCaseDepartmentClearanceLinesDto dto,int loggedInEmployeeId);

    }
}
