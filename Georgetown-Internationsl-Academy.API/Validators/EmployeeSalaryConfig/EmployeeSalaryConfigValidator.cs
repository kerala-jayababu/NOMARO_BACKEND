using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.EmployeeSalaryConfig
{
    public class EmployeeSalaryConfigValidator : AbstractValidator<EmployeeSalaryConfigDto>
    {
        public EmployeeSalaryConfigValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0).WithMessage("Employee ID is required.");
            RuleFor(x => x.ValidFrom).NotEmpty().WithMessage("ValidFrom is required.");           
            RuleFor(x => x.ValidTo).NotEmpty().WithMessage("ValidTo is required.");           
            RuleFor(x => x.ActiveStatus).NotEmpty().WithMessage("ActiveStatus is required.");
            RuleFor(x => x.TotalEarnings).GreaterThan(0).WithMessage("TotalEarnings is required.");
            RuleFor(x => x.TotalDeductions).GreaterThan(0).WithMessage("TotalDeductions is required.");
            RuleFor(x => x.NetSalary).GreaterThan(0).WithMessage("NetSalary is required.");
            


        }
    }
}
