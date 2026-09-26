using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface IApprovalWorkflowService
    {
        Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status,decimal? LeavePassageAmount,  string? rejectReason, int count = 1);
        Task<IEnumerable<ConfigApprovalsDto>> GetConfigApprovalsList(DateTime fromDate,string? actionStatus = null,string? entityCode = null,string? targetIdEmployee = null);
        Task AddUpdateWorkFlowApprovalForLeave(int idLeaveApplication, int idEmployee, EmployeeLeaveSetupDetailDto empLvConfigDetails);
    }
}

