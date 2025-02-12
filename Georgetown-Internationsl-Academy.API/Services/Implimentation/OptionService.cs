using AutoMapper;
using Dapper;
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
        private readonly IMapper _mapper;

        public OptionService(ApplicationDBContext dbContext, ILogger<OptionService> logger,IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
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

                // Fetch Employee Codes
                //var employee = await _dbContext.Employees
                //    .Select(b => new SelectOptionIntDto
                //    {
                //        Value = b.IdEmployee,
                //        DisplayName = $"{b.FirstName} {b.MiddleName} {b.LastName} "
                //    })
                //    .ToListAsync();

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
                    //EmployeeList=employee,
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

        public async Task<List<SalaryMonthsDto>> GetAllSalaryMonths()
        {
            try
            {
                var salaryMonths = await _dbContext.SalaryMonths.ToListAsync();
                return _mapper.Map<List<SalaryMonthsDto>>(salaryMonths);
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetAllSalaryMonths.");
                throw;
            }
        }
        public async Task<List<FinancialYearsDto>> GetAllFiancialyear()
        {
            try
            {
                var financialYears = await _dbContext.FinancialYears.ToListAsync();
                return _mapper.Map<List<FinancialYearsDto>>(financialYears);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetAllFiancialyear.");
                throw;
            }
        }
        

        public async Task<List<LatestEmployeeSalaryConfigDto>> GetEmployeeLatestSalaryStructure()
        {
            var query = @"
    SELECT 
        IdEmployeeSalaryConfig,
        IdEmployee,
        FirstName,
        MiddleName,
        LastName,
        ValidFrom,
        ValidTo,
        IdSalaryTemplate,
        CreatedBy,
        CreatedOn,
        ApprovalStatus,
        ActiveStatus,
        TotalEarnings,
        TotalDeductions,
        NetSalary
    FROM vw_LatestEmployeeSalaryConfig";

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<LatestEmployeeSalaryConfigDto>(query);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee latest salary structure.");
                throw new Exception("An error occurred while fetching the latest salary structure. Please try again later.", ex);
            }
        }


        public async Task<List<HolidayTypeDto>> GetHolidayTypes()
        {
            try
            {
                var salaryMonths = await _dbContext.HolidayTypes.ToListAsync();
                return _mapper.Map<List<HolidayTypeDto>>(salaryMonths);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetHolidayTypes.");
                throw;
            }
        }
    }
}
