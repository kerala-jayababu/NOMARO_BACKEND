using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class BankServices : IBankServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BankServices> _logger;

        public BankServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<IEnumerable<BankDto>> GetBanksList()
        {
            try
            {
                var banks = await _dbContext.Banks
         .OrderBy(b => b.BankName)  
         .ToListAsync();
                var bankDtos = _mapper.Map<IEnumerable<BankDto>>(banks);

                return bankDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of Banks.");
                throw;
            }
        }
        public async Task<IEnumerable<BankBranchesDto>> GetBranchesOfBank(int idBank)
        {
            try
            {
                var branches = await (from branch in _dbContext.BankBranches
                                      join bank in _dbContext.Banks on branch.IdBank equals bank.IdBank
                                      where branch.IdBank == idBank
                                      orderby branch.BranchName
                                      select new BankBranchesDto
                                      {
                                          IdBankBranches = branch.IdBankBranches,
                                          IdBank = branch.IdBank,
                                          BranchName = branch.BranchName,
                                          BankAddress = branch.BankAddress,
                                          PhoneNumber = branch.PhoneNumber,
                                          ABARoutingNumber = branch.ABARoutingNumber,
                                          BankName = bank.BankName 
                                      }).ToListAsync();

                return branches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching branches for bank ID {idBank}.");
                throw;
            }
        }


        public async Task<bool> AddOrUpdateBranchesOfBank(List<BankBranchesDto> bankBranchesDtoList)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var bankId = bankBranchesDtoList.FirstOrDefault()?.IdBank;

                if (bankId == null || bankId <= 0)
                {
                    return false; // Invalid input
                }

                // Fetch existing branches for the given bank
                var existingBranches = await _dbContext.BankBranches
                    .Where(b => b.IdBank == bankId)
                    .ToListAsync();

                var updatedBranches = new List<BankBranches>();

                // Process input branches
                foreach (var branchDto in bankBranchesDtoList)
                {
                    var existingBranch = existingBranches.FirstOrDefault(b => b.IdBankBranches == branchDto.IdBankBranches);

                    if (existingBranch != null)
                    {
                        // Update existing branch
                        existingBranch.BranchName = branchDto.BranchName;
                        existingBranch.BankAddress = branchDto.BankAddress;
                        existingBranch.PhoneNumber = branchDto.PhoneNumber;
                        existingBranch.ABARoutingNumber = branchDto.ABARoutingNumber;

                        updatedBranches.Add(existingBranch); // ✅ Add updated branches here
                    }
                    else
                    {
                        // Add new branch
                        var newBranch = new BankBranches
                        {
                            IdBank = branchDto.IdBank,
                            BranchName = branchDto.BranchName,
                            BankAddress = branchDto.BankAddress,
                            PhoneNumber = branchDto.PhoneNumber,
                            ABARoutingNumber = branchDto.ABARoutingNumber
                        };

                        await _dbContext.BankBranches.AddAsync(newBranch);
                        updatedBranches.Add(newBranch);
                    }
                }

                // Identify branches to remove (existing ones that are not in the input list)
                var inputBranchIds = bankBranchesDtoList.Select(b => b.IdBankBranches).ToList();
                var branchesToRemove = existingBranches.Where(b => !inputBranchIds.Contains(b.IdBankBranches)).ToList();

                if (branchesToRemove.Any())
                {
                    _dbContext.BankBranches.RemoveRange(branchesToRemove);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true; // ✅ Now returning true instead of a list
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing bank branches.");
                throw new Exception("An error occurred while processing bank branches. Please try again.");
            }
        }


    }
}
