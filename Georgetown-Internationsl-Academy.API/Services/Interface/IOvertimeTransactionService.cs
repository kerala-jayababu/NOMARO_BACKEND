using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IOvertimeTransactionService
    {
        Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionList(int EmployeeId,string? searchText,DateTime? startDate, string? dropdownFilter = null);
        Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id);
        Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transaction,int IdEmployee);
        Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transaction, int IdEmployee);
    }
}
