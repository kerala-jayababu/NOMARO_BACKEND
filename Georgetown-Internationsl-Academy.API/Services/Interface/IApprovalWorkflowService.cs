using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IApprovalWorkflowService
    {
        Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status, string? rejectReason);
        Task<IEnumerable<ConfigApprovalsDto>> GetConfigApprovalsList(DateTime fromDate,string? actionStatus = null,string? entityCode = null,string? targetIdEmployee = null);
    }
}
