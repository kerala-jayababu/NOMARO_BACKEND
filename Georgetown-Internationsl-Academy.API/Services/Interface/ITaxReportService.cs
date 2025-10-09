namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface ITaxReportService
    {
        Task<byte[]> GenerateTaxReportsAsync(List<int> employeeIds, int financialYear);
    }
}
