using AutoMapper;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Shift;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Microsoft.Extensions.Logging;

namespace Georgetown_Internationsl_Academy.API.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap< BudgetCodeEntity, BudgetCodeDto>().ReverseMap();
            CreateMap<DesignationEntity, DesignationDto>().ReverseMap();
            CreateMap<DepartmentEntity, DepartmentDto>().ReverseMap();
            CreateMap<SalaryHeads, SalaryHeadDto>().ReverseMap();
            CreateMap<Employee, EmployeeDto>().ReverseMap();
            CreateMap<EmployeePermissions, EmployeePermissionDto>().ReverseMap();
            CreateMap<RoleBasedPermission, RoleBasedPermissionDto>().ReverseMap();
            CreateMap<SystemParameter, SystemParameterDto>().ReverseMap();
            CreateMap<NotificationConfig, NotificationConfigDto>().ReverseMap();
            CreateMap<VacationMode, VacationModeDto>().ReverseMap();          
            CreateMap<TaxSlab, TaxSlabDto>().ReverseMap();
            CreateMap<CurrencyConversion, CurrencyConversionDto>().ReverseMap();
            CreateMap<ChildTaxThreshold, ChildTaxThresholdDto>().ReverseMap();
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
            CreateMap<SalaryMonths, SalaryMonthsDto>().ReverseMap();
            CreateMap<MaternityLeaveSalaryDetail, MaternityLeaveSalaryDetailDto>().ReverseMap();
            CreateMap<HolidayTypeEntity, HolidayTypeDto>().ReverseMap();
            CreateMap<FinancialYears, FinancialYearsDto>().ReverseMap();
            CreateMap<Banks, BankDto>().ReverseMap();
            CreateMap<BankBranches, BankBranchesDto>().ReverseMap();
            CreateMap<RentFreeQuarterDurations, RentFreeQuarterDurationsDto>().ReverseMap();
            CreateMap<LeavePassage, LeavePassageDto>().ReverseMap();
            CreateMap<Notification, NotificationDto>().ReverseMap();
            CreateMap<Holiday, HolidaysDto>().ReverseMap();

            #region Time & Attendance
            CreateMap<ShiftDefinitionEntity, ShiftDto>().ReverseMap();
            CreateMap<ShiftSchedule, ShiftScheduleDto>().ReverseMap();
            CreateMap<ShiftEmployee, ShiftEmployeeDto>().ReverseMap();
            CreateMap<ShiftAssignment, ShiftAssignmentDto>().ReverseMap();
            #endregion
        }
    }
}
