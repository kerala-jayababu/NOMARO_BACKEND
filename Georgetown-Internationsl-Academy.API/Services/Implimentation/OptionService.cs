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
    WITH LatestSalary AS (
        SELECT 
            lsc.IdEmployeeSalaryConfig,
            lsc.IdEmployee,
            lsc.FirstName,
            lsc.MiddleName,
            lsc.LastName,
            lsc.ValidFrom,
            lsc.ValidTo,
            lsc.IdSalaryTemplate,
            lsc.CreatedBy,
            lsc.CreatedOn,
            lsc.ApprovalStatus,
            lsc.ActiveStatus,
            lsc.TotalEarnings,
            lsc.TotalDeductions,
            lsc.NetSalary
        FROM vw_LatestEmployeeSalaryConfig lsc
    )
    SELECT 
        ls.IdEmployeeSalaryConfig,
        ls.IdEmployee,
        ls.FirstName,
        ls.MiddleName,
        ls.LastName,
        ls.ValidFrom,
        ls.ValidTo,
        ls.IdSalaryTemplate,
        ls.CreatedBy,
        ls.CreatedOn,
        ls.ApprovalStatus,
        ls.ActiveStatus,
        ls.TotalEarnings,
        ls.TotalDeductions,
        ls.NetSalary,
        escd.IdSalaryHead,
        sh.SalaryHeadCode,
        sh.SalaryHeadName,
        sh.HeadType,
        sh.IsTaxable,
        sh.IsActive,
        sh.CalculationMethod,
        sh.IdPercentageSalaryHead,
        sh.PercentageValue,
        sh.FixedValue,
        sh.CustomFormula,
        sh.OrderNumber,
        escd.SalaryAmount  
    FROM LatestSalary ls
    LEFT JOIN EmployeeSalaryConfigDetails escd ON ls.IdEmployeeSalaryConfig = escd.IdEmployeeSalaryConfig
    LEFT JOIN SalaryHeads sh ON escd.IdSalaryHead = sh.IdSalaryHead
    ORDER BY ls.IdEmployee, sh.OrderNumber;";

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync<LatestEmployeeSalaryConfigDto, SalaryComponentDto, LatestEmployeeSalaryConfigDto>(
                        query,
                        (salary, component) =>
                        {
                            var salaryStructure = salary;
                            if (salaryStructure.SalaryComponents == null)
                            {
                                salaryStructure.SalaryComponents = new List<SalaryComponentDto>();
                            }
                            if (component != null)
                            {
                                salaryStructure.SalaryComponents.Add(component);
                            }
                            return salaryStructure;
                        },
                        splitOn: "IdSalaryHead"
                    );

                    return result.GroupBy(s => s.IdEmployeeSalaryConfig)
                                 .Select(g =>
                                 {
                                     var grouped = g.First();
                                     grouped.SalaryComponents = g.SelectMany(s => s.SalaryComponents).ToList();
                                     return grouped;
                                 }).ToList();
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

        public async Task<List<NotificationDto>> GetEmployeeNotification(int employeeID)
        {
            try
            {
                var notification = await _dbContext.Notifications.Where(x=>x.ReceivedByIdEmployee == employeeID && x.IsReadAppNotification == false) .ToListAsync();
                return _mapper.Map<List<NotificationDto>>(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all EmployeeNotification by Id.");
                throw;
            }
        }

        public async Task<NotificationDto> UpdateEmployeeNotification(int idNotification)
        {
            try
            {
                var notification = await _dbContext.Notifications
                    .FirstOrDefaultAsync(x => x.IdNotification == idNotification);

                if (notification != null)
                {
                    notification.IsReadAppNotification = true;
                    notification.ReadAt = DateTime.Now;
                    await _dbContext.SaveChangesAsync();

                    // Map and return the updated notification
                    return _mapper.Map<NotificationDto>(notification);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating notification with ID {idNotification}.");
                return null;
            }
        }

    }
}
