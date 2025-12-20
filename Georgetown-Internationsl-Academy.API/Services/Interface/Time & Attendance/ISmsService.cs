namespace Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance
{
    public interface ISmsService
    {
        Task<bool> SendSmsMissingEntryAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime exitDateTime);
        Task<bool> SendSmsMissingExitAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime entryDateTime);
        Task<bool> SendSmsUnauthorizedAbsenceAsync(int idEmployee, int loggedInEmployeeId, string mobileNumber, string employeeName, DateTime absentDate);
        Task SendSMSAsync(string mobileNumber, string message);
    }
}
