using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.RoleBasedScreenPermission
{
    public class PayrollScreenDtoValidator : AbstractValidator<PayrollScreenDto>
    {
        public PayrollScreenDtoValidator()
        {
            RuleFor(x => x.ScreenName)
                .NotEmpty().WithMessage("ScreenName is required.")
                .MaximumLength(50).WithMessage("ScreenName must not exceed 50 characters.");

            RuleFor(x => x.ValidPermissions)
      .Must(permission =>
      {
          if (string.IsNullOrEmpty(permission))
              return true; // Allow null or empty values

          var allowedValues = new[] { 'V', 'A', 'U', 'D' };
          return permission.All(c => allowedValues.Contains(c));
      })
      .WithMessage("ValidPermissions can only contain the characters 'V', 'A', 'U', or 'D'.");


            RuleFor(x => x.IdParentPayrollScreen)
                .GreaterThan(0).When(x => x.IdParentPayrollScreen.HasValue)
                .WithMessage("IdParentPayrollScreen must be greater than 0 if provided.");
        }
    }


}
