using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.Employee
{
    public class EmployeeEntityDtoValidator : AbstractValidator<EmployeeEntityDto>
    {
        public EmployeeEntityDtoValidator()
        {
            RuleFor(e => e.EmployeeCode)
                .NotEmpty().WithMessage("Employee code is required.")
                .MaximumLength(50).WithMessage("Employee code must not exceed 50 characters.");

            RuleFor(e => e.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");
      

            RuleFor(e => e.EmailID)
                .EmailAddress().When(e => !string.IsNullOrEmpty(e.EmailID))
                .WithMessage("Invalid email format.");

            RuleFor(e => e.EmployeePhoto)
                .Must(f => f == null || f.Length <= 2 * 1024 * 1024)
                .WithMessage("Employee photo must not exceed 2MB.");
        }
    }
}
