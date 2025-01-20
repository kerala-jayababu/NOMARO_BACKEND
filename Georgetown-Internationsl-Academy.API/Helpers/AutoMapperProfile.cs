using AutoMapper;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
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
            CreateMap<TaxConfig, TaxConfigDto>().ReverseMap();
            CreateMap<TaxConfig, TaxConfigManageDto>().ReverseMap();
            CreateMap<TaxSlab, TaxSlabDto>().ReverseMap();
            CreateMap<CurrencyConversion, CurrencyConversionDto>().ReverseMap();

        }
     }
}
