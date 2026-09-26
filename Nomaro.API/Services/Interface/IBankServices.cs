using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IBankServices
    {
        Task<IEnumerable<BankDto>> GetBanksList();
        Task<IEnumerable<BankBranchesDto>> GetBranchesOfBank(int idBank);
        Task<bool> AddOrUpdateBranchesOfBank(List<BankBranchesDto> bankBranchesDtoList);
        Task<bool> AddOrUpdateBanks(List<BankDto> bankDtoList);
    }
}

