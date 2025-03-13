using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IBankServices
    {
        Task<IEnumerable<BankDto>> GetBanksList();
        Task<IEnumerable<BankBranchesDto>> GetBranchesOfBank(int idBank);
        Task<bool> AddOrUpdateBranchesOfBank(List<BankBranchesDto> bankBranchesDtoList);
    }
}
