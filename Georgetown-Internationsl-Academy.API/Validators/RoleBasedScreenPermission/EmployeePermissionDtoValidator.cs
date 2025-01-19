using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.RoleBasedScreenPermission
{
    public class EmployeePermissionDtoValidator : AbstractValidator<EmployeePermissionDto>
    {
        public EmployeePermissionDtoValidator()
        {
            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("IdEmployee is required and must be greater than 0.");

            RuleFor(x => x.IdPayrollScreen)
                .GreaterThan(0).WithMessage("IdPayrollScreen is required and must be greater than 0.");

            RuleFor(x => x.Permission)
                .NotEmpty().WithMessage("Permission is required.")
                .Must(permission =>
                {
                    var allowedValues = new[] { 'V', 'A', 'U', 'D' };
                    return permission.All(c => allowedValues.Contains(c));
                })
                .WithMessage("Permission can only contain the characters 'V', 'A', 'U', or 'D'.");
        }
    }

}
