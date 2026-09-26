using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface IReportServices
    {
        Task<IEnumerable<ReportsMasterDto>> GetReportMasters();
        Task<IEnumerable<ReportConditionsDto>> GetReportConditionById(int id);
        Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(StoredProcedureDto request);
        Task<List<dynamic>> GetReportsTableValue(string tableName, string valueColumn, string displayColumn);
        Task<IEnumerable<ReportColumnsDto>> GetReportColumnsById(int IdReport);
        Task<byte[]> GenerateIncomeTaxReportAsync(int payrollId, CompanyDetails company, string taxMonth);
        Task<byte[]> GenerateNISReportAsync(int payrollId, string ageGroup, CompanyDetails companyDetails, string salaryMonth);

    }
}

