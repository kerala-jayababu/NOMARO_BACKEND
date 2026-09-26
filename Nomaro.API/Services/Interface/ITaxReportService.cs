namespace Nomaro.API.Services.Interface
{
    public interface ITaxReportService
    {
        Task<byte[]> GenerateTaxReportsAsync(List<int> employeeIds, int financialYear);
    }
}

