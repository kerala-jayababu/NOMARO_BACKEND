using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class OptionService : IOptionService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<OptionService> _logger;

        public OptionService(ApplicationDBContext dbContext, ILogger<OptionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<AllOptionsDto> GetAllOptions()
        {
            try
            {
                // Fetch Salary Heads
                var salaryHeads = await _dbContext.SalaryHeads
                    .Select(s => new SelectOptionIntDto
                    {
                        Value = s.IdSalaryHead,
                        DisplayName = s.SalaryHeadName
                    })
                    .ToListAsync();

                // Fetch Budget Codes
                var budgetCodes = await _dbContext.BudgetCodes
                    .Select(b => new SelectOptionIntDto
                    {
                        Value = b.IdBudgetCode,
                        DisplayName = b.BudgetCodeName
                    })
                    .ToListAsync();

                // Fetch Banks
                var banks = await _dbContext.Banks
                    .Select(b => new SelectOptionIntDto
                    {
                        Value = b.IdBank,
                        DisplayName = b.BankName
                    })
                    .ToListAsync();

                var branches = await _dbContext.BankBranches
                   .Select(b => new SelectOptionIntDto
                   {
                       Value = b.IdBankBranches,
                       DisplayName = b.BranchName
                   })
                   .ToListAsync();

                var overtime = await _dbContext.OvertimeTypes
                   .Select(b => new SelectOptionIntDto
                   {
                       Value = b.IdOvertimeType,
                       DisplayName = b.OvertimeTypeName
                   })
                   .ToListAsync();


                // Return all options together
                return new AllOptionsDto
                {
                    SalaryHeads = salaryHeads,
                    BudgetCodes = budgetCodes,
                    Banks = banks,
                    BankBranches= branches,
                    OverTimesTypes = overtime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all options.");
                throw;
            }
        }





    }
}
