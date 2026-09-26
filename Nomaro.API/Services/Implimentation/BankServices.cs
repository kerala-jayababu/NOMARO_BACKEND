using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Implementation;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Nomaro.API.Services.Implimentation
{
    public class BankServices : IBankServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BankServices> _logger;
        private readonly IAuditService _auditService;

        public BankServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
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
                    var branchIdsToRemove = branchesToRemove.Select(b => b.IdBankBranches).ToList();
                    var blockedBranchIds = await _dbContext.EmployeeBankAccounts
                      .Where(eba => branchIdsToRemove.Contains((int)eba.IdBankBranch))
                      .Select(eba => eba.IdBankBranch)
                      .Distinct()
                      .ToListAsync();
                    // Allowed to delete = branchesToRemove - blockedBranchIds
                    var allowedToDelete = branchesToRemove
                        .Where(b => !blockedBranchIds.Contains(b.IdBankBranches))
                        .ToList();
                    if (allowedToDelete.Any())
                    {
                        _dbContext.BankBranches.RemoveRange(allowedToDelete);
                    }

                  
                }

                var beforeUpdate = existingBranches.Select(b => new
                {
                    b.IdBankBranches,
                    b.IdBank,
                    b.BranchName,
                    b.BankAddress,
                    b.PhoneNumber,
                    b.ABARoutingNumber
                }).ToList();

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "BankBranch",
                    entityId: bankId.GetValueOrDefault(),
                    actionDetails: new { before = beforeUpdate, after = new { bankId, branchCount = updatedBranches.Count, inputCount = bankBranchesDtoList.Count } });

                return true; // ✅ Now returning true instead of a list
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing bank branches.");
                throw new Exception("An error occurred while processing bank branches. Please try again.");
            }
        }


        public async Task<bool> AddOrUpdateBanks(List<BankDto> bankDtoList)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingBanks = await _dbContext.Banks.ToListAsync();
                var updatedBanks = new List<Banks>();

                foreach (var bankDto in bankDtoList)
                {
                    var existingBank = existingBanks.FirstOrDefault(b => b.IdBank == bankDto.IdBank);

                    if (existingBank != null)
                    {
                        // Update existing
                        existingBank.BankName = bankDto.BankName;
                        existingBank.SwiftCode = bankDto.SwiftCode;

                        updatedBanks.Add(existingBank);
                    }
                    else
                    {
                        // Add new
                        var newBank = new Banks
                        {
                            BankName = bankDto.BankName,
                            SwiftCode = bankDto.SwiftCode
                        };

                        await _dbContext.Banks.AddAsync(newBank);
                        updatedBanks.Add(newBank);
                    }
                }

                // Delete banks that are not in the input list
                //var inputBankIds = bankDtoList.Where(b => b.IdBank > 0).Select(b => b.IdBank).ToList();
                //var banksToRemove = existingBanks.Where(b => !inputBankIds.Contains(b.IdBank)).ToList();

                //if (banksToRemove.Any())
                //{
                //    _dbContext.Banks.RemoveRange(banksToRemove);
                //}

                var beforeUpdateBanks = existingBanks.Select(b => new { b.IdBank, b.BankName, b.SwiftCode }).ToList();

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "Bank",
                   entityId: updatedBanks.FirstOrDefault()?.IdBank ?? 0,
                    actionDetails: new { before = beforeUpdateBanks, after = new { bankCount = bankDtoList.Count, inputBanks = bankDtoList.Select(b => new { b.IdBank, b.BankName, b.SwiftCode }) } });

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing banks.");
                throw new Exception("An error occurred while processing banks. Please try again.");
            }
        }


    }
}

