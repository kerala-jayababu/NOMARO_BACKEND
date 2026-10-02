using AutoMapper;
using Nomaro.API.DTO;
using Nomaro.API.DTO.Shift;
using Nomaro.API.DTO.Time___Attendance.Shift;
using Nomaro.API.Models;
using Nomaro.API.Models.Shift;
using Nomaro.API.Models.Time___Attendance.Shift;
using Microsoft.Extensions.Logging;

namespace Nomaro.API.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap< BudgetCodeEntity, BudgetCodeDto>().ReverseMap();
            CreateMap<DesignationEntity, DesignationDto>().ReverseMap();
            CreateMap<DepartmentEntity, DepartmentDto>().ReverseMap();
            CreateMap<SalaryHeads, SalaryHeadDto>().ReverseMap();
            CreateMap<Employee, Nomaro.API.DTO.EmployeeDto>().ReverseMap();
            CreateMap<EmployeePermissions, EmployeePermissionDto>().ReverseMap();
            CreateMap<RoleBasedPermission, RoleBasedPermissionDto>().ReverseMap();
            CreateMap<SystemParameter, SystemParameterDto>().ReverseMap();
            CreateMap<NotificationConfig, NotificationConfigDto>().ReverseMap();
            CreateMap<VacationMode, VacationModeDto>().ReverseMap();          
            CreateMap<TaxSlab, TaxSlabDto>().ReverseMap();
            CreateMap<CurrencyConversion, CurrencyConversionDto>().ReverseMap();
            CreateMap<TaxYearConfigs, TaxYearConfigDto>().ReverseMap();
            CreateMap<SalaryTemplate, SalaryTemplateDto>().ReverseMap();
            CreateMap<SalaryTemplate, SalaryTemplateManageDto>().ReverseMap();
            CreateMap<ScheduledDeductionDetailsDto, ScheduledDeductionDetails>().ReverseMap();
            CreateMap<OvertimeTransactionEntity, OvertimeTransactionDto>().ReverseMap();
            CreateMap<SalaryTemplateDetails, SalaryTemplateDetailDto>().ReverseMap();
            CreateMap<SalaryAdjustment, SalaryAdjustmentDto>().ReverseMap();
            CreateMap<ScheduledSalaryDeduction, ScheduledSalaryDeductionDto>().ReverseMap();
            CreateMap<MaternityLeaveSalaryEntity, MaternityLeaveSalaryDto>().ReverseMap();
            CreateMap<RentFreeQuarter, RentFreeQuarterDto>().ReverseMap();
            CreateMap<EmployeeSalaryConfig, EmployeeSalaryConfigDto>().ReverseMap();
            CreateMap<EmployeeSalaryConfigDetails, EmployeeSalaryConfigDetailsDto>().ReverseMap();
            CreateMap<EmployeeSalaryConfigDto, EmployeeSalaryConfig>()
                 .ForMember(dest => dest.EmployeeSalaryConfigDetails, opt => opt.Ignore());
            CreateMap<SalaryMonths, SalaryMonthsDto>().ReverseMap();
            CreateMap<MaternityLeaveSalaryDetail, MaternityLeaveSalaryDetailDto>().ReverseMap();
            CreateMap<HolidayTypeEntity, HolidayTypeDto>().ReverseMap();
            CreateMap<FinancialYears, FinancialYearsDto>().ReverseMap();
            CreateMap<WorkYears, WorkYearsDto>().ReverseMap();
            CreateMap<EmployeeTypes, EmployeeTypeDto>().ReverseMap(); 
            CreateMap<Banks, BankDto>().ReverseMap();
            CreateMap<BankBranches, BankBranchesDto>().ReverseMap();
            CreateMap<RentFreeQuarterDurations, RentFreeQuarterDurationsDto>().ReverseMap();
            CreateMap<LeavePassage, LeavePassageDto>().ReverseMap();
            CreateMap<Notification, NotificationDto>().ReverseMap();
            CreateMap<Holiday, HolidaysDto>().ReverseMap();
            CreateMap<Employee, EmployeeEntityDto>().ReverseMap();
            CreateMap<AssetTypes, AssetTypeDto>().ReverseMap();
            CreateMap<Assets, AssetDto>().ReverseMap();
            CreateMap<ExitTypes, ExitTypeDto>().ReverseMap();
            CreateMap<OfficeTypes, OfficeTypeDto>().ReverseMap();
            CreateMap<Offices, OfficeDto>().ReverseMap();
            CreateMap<EmployeeOfficePostings, EmployeeOfficePostingDto>().ReverseMap();
            CreateMap<EmployeeStatutoryDetails, EmployeeStatutoryDetailsDto>().ReverseMap();
            CreateMap<States, StateDto>().ReverseMap();
            
            #region Time & Attendance
            CreateMap<ShiftDefinitionEntity, ShiftDto>().ReverseMap();
            CreateMap<ShiftSchedule, ShiftScheduleDto>().ReverseMap();
            CreateMap<ShiftEmployee, ShiftEmployeeDto>().ReverseMap();
            CreateMap<ShiftAssignment, ShiftAssignmentDto>().ReverseMap();

            CreateMap<ExitReasons, ExitReasonDto>().ReverseMap();     
            CreateMap<NoticePeriodPolicies, NoticePeriodPolicyDto>().ReverseMap();
            CreateMap<ClearanceTemplates, ClearanceTemplateDto>().ReverseMap();
            CreateMap<ClearanceTemplateDepartments, ClearanceTemplateDepartmentDto>().ReverseMap();
            #endregion
        }
    }
}

