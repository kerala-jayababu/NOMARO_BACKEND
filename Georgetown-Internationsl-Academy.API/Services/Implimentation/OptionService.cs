using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
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

        public async Task<List<SelectOptionIntDto>> GetSalaryHeadsOptions()
        {
            try
            {
                // Fetch the relevant fields from the database
                var salaryHeads = await _dbContext.SalaryHeads
                    .Select(s => new SelectOptionIntDto
                    {
                        Value = s.IdSalaryHead,
                        DisplayName = s.SalaryHeadName
                    })
                    .ToListAsync();

                return salaryHeads?.Any() == true ? salaryHeads : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of salary heads.");
                throw;
            }
        }
        public async Task<List<SelectOptionIntDto>> GetBudgetCodeOptions()
        {
            try
            {
                // Fetch the relevant fields from the database
                var budgetHeads = await _dbContext.BudgetCodes
                    .Select(s => new SelectOptionIntDto
                    {
                        Value = s.IdBudgetCode,
                        DisplayName = s.BudgetCodeName
                    })
                    .ToListAsync();

                return budgetHeads?.Any() == true ? budgetHeads : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of budget code.");
                throw;
            }
        }

        public async Task<List<SelectOptionIntDto>> GetBanksOptions()
        {
            try
            {
                // Fetch the relevant fields from the database
                var banks = await _dbContext.Banks
                    .Select(s => new SelectOptionIntDto
                    {
                        Value = s.IdBank,
                        DisplayName = s.BankName
                    })
                    .ToListAsync();

                return banks?.Any() == true ? banks : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of banks.");
                throw;
            }
        }

        


    }
}
