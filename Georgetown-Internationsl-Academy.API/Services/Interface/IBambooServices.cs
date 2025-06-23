using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IBambooServices
    {
        Task<List<BambooHRDetailsDto>> SyncEmployeesFromBambooHR();

        Task<string> SyncTimeOffRequests(DateTime start, DateTime end);
    }
}
