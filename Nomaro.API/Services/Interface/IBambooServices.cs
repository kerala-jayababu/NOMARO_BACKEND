using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IBambooServices
    {
        Task<List<BambooHRDetailsDto>> SyncEmployeesFromBambooHR();

        Task<string> SyncTimeOffRequests(DateTime start, DateTime end);
        Task<DateTime?> BambooHRLeaveIntegrationLastRun();
        Task<string> SyncTimeOffRequestsForLeave(DateTime start, DateTime end);

    }
}

