using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IReportServices
    {
        Task<IEnumerable<ReportsMasterDto>> GetReportMasters();
        Task<IEnumerable<ReportConditionsDto>> GetReportConditionById(int id);
        Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(StoredProcedureDto request);
        Task<List<dynamic>> GetReportsTableValue(string tableName, string valueColumn, string displayColumn);
        

    }
}
