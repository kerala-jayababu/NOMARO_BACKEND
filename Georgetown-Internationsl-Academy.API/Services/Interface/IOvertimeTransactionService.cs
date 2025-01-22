using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IOvertimeTransactionService
    {
        Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionList();
        Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id);
        Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transaction);
        Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transaction);
    }
}
