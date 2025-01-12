using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
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
