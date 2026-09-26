using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Services.Interface
{
    public interface IBudgetCodeServices
    {
        #region BudgetCodes
        Task<IEnumerable<BudgetCodeDto>> GetBudgetList();   
        Task<BudgetCodeDto?> GetBudgetCodeByID(int id);
        Task<BudgetCodeDto?> AddBudgetCode(BudgetCodeDto budgetCode);
        Task<BudgetCodeDto?> UpdateBudgetCode(BudgetCodeDto budgetCode);
        #endregion

    }
}

