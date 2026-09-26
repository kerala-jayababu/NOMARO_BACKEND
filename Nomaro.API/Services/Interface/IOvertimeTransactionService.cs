using Nomaro.API.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Nomaro.API.Services.Interface
{
    public interface IOvertimeTransactionService
    {
        Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionList(int EmployeeId,string? searchText,DateTime? startDate, string? dropdownFilter = null);
        Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionsForSelfPortal(int EmployeeId,DateTime? startDate);
        
         Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id);
        Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transaction,int IdEmployee);
        Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionUpdateDto transaction, int IdEmployee);
        Task<decimal> GetOverTimeAmount(int IdEmployee, DateTime OvertimeDate, decimal DurationInHours);
        Task<bool> IsOverTimeTransactionAllowed(int IdEmployee);
        Task<IEnumerable<OvertimeTransactionFullDto>> GetOvertimeTransactionsFullDetails(int EmployeeId, DateTime? dateFrom, DateTime? dateTo);

    }
}

