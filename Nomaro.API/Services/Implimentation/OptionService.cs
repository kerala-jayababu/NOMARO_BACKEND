using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nomaro.API.Services.Implimentation
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

        public async Task<List<SalaryMonthsDto>> GetWorkYearSalaryMonths(int IdWorkYear)
        {
            try
            {
                var workYear = await _dbContext.WorkYears.Where(w=>w.IdWorkYear == IdWorkYear).FirstOrDefaultAsync();
                var salaryMonths = await _dbContext.SalaryMonths.Where(s=>s.SalaryMonthDate >= workYear.WorkDateFrom && s.SalaryMonthDate<= workYear.WorkDateTo).ToListAsync();
                return _mapper.Map<List<SalaryMonthsDto>>(salaryMonths);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetAllSalaryMonths.");
                throw;
            }
        }
        public async Task<List<EmployeeTypeDto>> GetEmployeeWorkTypes()
        {
            try
            {
                var salaryMonths = await _dbContext.EmployeeTypes.ToListAsync();
                return _mapper.Map<List<EmployeeTypeDto>>(salaryMonths);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetAllSalaryMonths.");
                throw;
            }
        }


        public async Task<dynamic> GetSalaryOptions()
        {
            try
            {
                var workFlowConfigId = await _dbContext.WorkFlowConfig
        .Where(c => c.EntityCode == "SALARYGEN")
        .Select(c => c.IdWorkFlowConfig)
        .FirstOrDefaultAsync();



                if (workFlowConfigId == 0)
                    return new List<dynamic>();

                var approvalStatuses = await _dbContext.WorkFlowConfigDetails
         .Where(d => d.IdWorkFlowConfig == workFlowConfigId)
         .Select(d => d.ApprovalStatusName)
         .Distinct()
         .ToListAsync();


                var options = approvalStatuses
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new { value = name?.ToUpper(), label = name })
            .Cast<dynamic>()
            .ToList();
                var staticStatuses = new List<dynamic>
        {
            new { value = "SUBMITTED", label = "Submitted" },
            new { value = "REJECTED", label = "Rejected" },
            new { value = "DRAFT GENERATED", label = "Draft Generated" },
            new { value = "NOT GENERATED", label = "Not Generated" }
        };

                options.AddRange(staticStatuses);
                options = options
    .GroupBy(o => o.value)
    .Select(g => g.First())
    .OrderBy(o => o.label) 
    .ToList();
                return options;
            }
            catch (Exception ex)
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

        public async Task<List<WorkYearsDto>> GetAllWorkYears()
        {
            try
            {
                var WorkYears = await _dbContext.WorkYears.ToListAsync();
                return _mapper.Map<List<WorkYearsDto>>(WorkYears);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all GetAllWorkYears.");
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

        public async Task<List<QualificationTypesDto>> GetQualificationTypes()
        {
            try
            {
                var qualificationTypes = await _dbContext.QualificationTypes.ToListAsync();
                var result = qualificationTypes.Select(q => new QualificationTypesDto
                {
                    IdQualificationType = q.IdQualificationType,
                    QualificationTypeName = q.QualificationTypeName
                }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all QualificationTypes.");
                throw;
            }
        }
        public async Task<List<NationalitiesDto>> GetNationalities()
        {
            try
            {
                var nationalities = await _dbContext.Nationalities.ToListAsync();

                var result = nationalities.Select(n => new NationalitiesDto
                {
                    Nationality = n.Nationality
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Nationalities.");
                throw;
            }
        }

        public async Task<List<NotificationDto>> GetEmployeeNotification(int employeeID)
        {
            try
            {
                var notification = await _dbContext.Notifications.Where(x=>x.ReceivedByIdEmployee == employeeID && x.IsReadAppNotification == false).OrderByDescending(x=>x.CreatedAt).ToListAsync();
                return _mapper.Map<List<NotificationDto>>(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all EmployeeNotification by Id.");
                throw;
            }
        }
        public async Task<List<WorkFlowConfig>> GetWorkflowConfigList()
        {
            try
            {
                var notification = await _dbContext.WorkFlowConfig.Where(x=>x.EntityCode!= "SALARYGEN").ToListAsync();
                return _mapper.Map<List<WorkFlowConfig>>(notification);
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

        public async Task<List<WorkFlowConfigDetails>> GetWorkflowConfigDetailsList(int entityId)
        {
            try
            {
                var notification = await _dbContext.WorkFlowConfigDetails
                    .Where(x => x.IdWorkFlowConfig == entityId)
                    .ToListAsync();

                // Hardcoded Submitted and Rejected
                notification.Insert(0, new WorkFlowConfigDetails
                {
                    IdWorkFlowConfigDetail = 0,
                    IdWorkFlowConfig = entityId,
                    LevelNumber = 0,
                    ApprovalAuthorityType = "System",
                    ApprovalAuthorityID = null,
                    ApprovalStatusName = "SUBMITTED"
                });

                notification.Add(new WorkFlowConfigDetails
                {
                    IdWorkFlowConfigDetail = -1,
                    IdWorkFlowConfig = entityId,
                    LevelNumber = 0,
                    ApprovalAuthorityType = "System",
                    ApprovalAuthorityID = null,
                    ApprovalStatusName = "REJECTED"
                });

                return notification;
            }
            catch (Exception ex)
            {
                // log exception here if needed
                return null;
            }
        }
        public async Task<IEnumerable<CountryDto>> GetCountries()
        {
            try
            {
                return await _dbContext.Countries
                    .OrderBy(x => x.CountryName)
                    .Select(x => new CountryDto
                    {
                        IdCountry = x.IdCountry,
                        CountryCode = x.CountryCode,
                        CountryName = x.CountryName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries list.");
                throw;
            }
        }
        public async Task<IEnumerable<DocumentTypeDto>> GetDocumentTypes()
        {
            try
            {
                return await _dbContext.DocumentTypes
                    .OrderBy(x => x.DocumentTypeName)
                    .Select(x => new DocumentTypeDto
                    {
                        IdDocumentType = x.IdDocumentType,
                        DocumentTypeName = x.DocumentTypeName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Document Types.");
                throw;
            }
        }

        public async Task<List<MobileNotifications>> GetMobileNotificationsForEmployee(int IdEmployee)
        {
            var mobNotifications = await _dbContext.MobileNotifications.
                Where(m => m.IdEmployee == IdEmployee && m.ReadStatus == false).ToListAsync();
            return mobNotifications;
        }
        public async Task<bool> UpdateMobileNotificationReadStatus(int idMobileNotification)
        {
            var notification = await _dbContext.MobileNotifications
                .FirstOrDefaultAsync(m => m.IdMobileNotification == idMobileNotification);

            if (notification == null)
                return false;

            notification.ReadStatus = true;
            notification.ReadDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            return true;
        }

    }
}

