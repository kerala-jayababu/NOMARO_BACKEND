namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IApprovalWorkflowService
    {
        Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status, string? rejectReason);
    }
}
