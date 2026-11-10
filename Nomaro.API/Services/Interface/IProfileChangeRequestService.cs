using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IProfileChangeRequestService
    {
        // Employee self-service
        Task<MyProfileDto?> GetMyProfile(int idEmployee);
        Task<ProfileChangeLookupsDto> GetLookups(bool activeOnly);
        Task<int> SaveRequest(int idEmployee, SaveProfileChangeRequestDto dto);
        Task<string> SubmitRequest(int idEmployee, int idRequest);
        Task CancelRequest(int idEmployee, int idRequest);
        Task<List<ProfileChangeRequestListDto>> GetMyRequests(int idEmployee);
        Task<FileDownloadDto?> GetMyEmployeeDocument(int idEmployee, int idEmployeeDocument);

        // Shared
        Task<ProfileChangeRequestDetailDto?> GetRequestDetail(int idRequest, int viewerId, bool canViewAll, bool canReveal);
        Task<FileDownloadDto?> GetRequestDocument(int idRequestDocument, int viewerId, bool canViewAll);

        // HR
        Task<ProfileChangeDashboardDto> GetDashboard(int viewerId, bool canViewAll);
        Task<List<ProfileChangeRequestListDto>> GetHrRequests(int viewerId, bool canViewAll, ProfileChangeHrFilterDto filter);
        Task<string> ActionRequest(int approverId, ProfileChangeActionDto dto);
        Task<string?> RevealValue(int idDetail, string which, int viewerId, bool canViewAll);
        Task<List<ProfileChangeHistoryEntryDto>> GetEmployeeChangeHistory(int idEmployee);

        // Settings
        Task UpdateRequestType(ProfileChangeRequestTypeUpdateDto dto, int userId);
        Task<List<ProfileChangeWorkflowDto>> GetWorkflows();
        Task SaveWorkflow(ProfileChangeWorkflowDto dto);
        Task<ProfileChangeWorkflowLookupsDto> GetWorkflowLookups();
    }

    // Thrown when the master record changed after the employee submitted; HR must confirm.
    public class ProfileChangeConflictException : InvalidOperationException
    {
        public ProfileChangeConflictException(string message) : base(message) { }
    }
}
