using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.RoleBasedScreenPermission
{
    public class RoleBasedPermissionDtoValidator : AbstractValidator<RoleBasedPermissionDto>
    {
        public RoleBasedPermissionDtoValidator()
        {
            RuleFor(x => x.IdDesignation)
                .GreaterThan(0).WithMessage("IdDesignation is required and must be greater than 0.");

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
