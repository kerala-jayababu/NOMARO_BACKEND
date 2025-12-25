using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IOptionService
    {
        Task<AllOptionsDto> GetAllOptions();
        Task<dynamic> GetSalaryOptions();        
        Task<List<SalaryMonthsDto>> GetAllSalaryMonths();
        Task<List<NotificationDto>> GetEmployeeNotification(int employeeID);
        Task<NotificationDto> UpdateEmployeeNotification(int idNotification);        
        Task<List<FinancialYearsDto>> GetAllFiancialyear();        
        Task<List<HolidayTypeDto>> GetHolidayTypes();
        Task<List<LatestEmployeeSalaryConfigDto>> GetEmployeeLatestSalaryStructure();
        Task<List<WorkFlowConfig?>> GetWorkflowConfigList();
        Task<List<WorkFlowConfigDetails>> GetWorkflowConfigDetailsList(int entityId);
        Task<List<QualificationTypesDto>> GetQualificationTypes();
        Task<IEnumerable<CountryDto>> GetCountries();
    }
}
