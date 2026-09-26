using Nomaro.API.DTO;
using Nomaro.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nomaro.API.Services.Interface
{
    public interface IOptionService
    {
        Task<AllOptionsDto> GetAllOptions();
        Task<dynamic> GetSalaryOptions();        
        Task<List<SalaryMonthsDto>> GetAllSalaryMonths();
        Task<List<NotificationDto>> GetEmployeeNotification(int employeeID);
        Task<NotificationDto> UpdateEmployeeNotification(int idNotification);        
        Task<List<FinancialYearsDto>> GetAllFiancialyear();
        Task<List<WorkYearsDto>> GetAllWorkYears();
        Task<List<HolidayTypeDto>> GetHolidayTypes();
        Task<List<LatestEmployeeSalaryConfigDto>> GetEmployeeLatestSalaryStructure();
        Task<List<WorkFlowConfig?>> GetWorkflowConfigList();
        Task<List<WorkFlowConfigDetails>> GetWorkflowConfigDetailsList(int entityId);
        Task<List<QualificationTypesDto>> GetQualificationTypes();
        Task<List<NationalitiesDto>> GetNationalities();
        Task<IEnumerable<CountryDto>> GetCountries();
        Task<IEnumerable<DocumentTypeDto>> GetDocumentTypes();
        Task<List<EmployeeTypeDto>> GetEmployeeWorkTypes();
        Task<List<MobileNotifications>> GetMobileNotificationsForEmployee(int IdEmployee);
        Task<bool> UpdateMobileNotificationReadStatus(int idMobileNotification);
        Task<List<SalaryMonthsDto>> GetWorkYearSalaryMonths(int IdWorkYear);
    }

}

