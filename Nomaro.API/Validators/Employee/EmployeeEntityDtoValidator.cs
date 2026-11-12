using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.Employee
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

            RuleFor(e => e.VisaNumber)
                .MaximumLength(50).WithMessage("Residence Visa File No. must not exceed 50 characters.");

            RuleFor(e => e.VisaSponsor)
                .MaximumLength(30).WithMessage("Visa Sponsor must not exceed 30 characters.");

            RuleFor(e => e.LabourCardNumber)
                .MaximumLength(30).WithMessage("Labour Card No. must not exceed 30 characters.")
                .Matches("^[0-9]+$").When(e => !string.IsNullOrWhiteSpace(e.LabourCardNumber))
                .WithMessage("Labour Card / MOHRE Person ID must contain digits only.");

            RuleFor(e => e.EmployeePhoto)
                .Must(f => f == null || f.Length <= 2 * 1024 * 1024)
                .WithMessage("Employee photo must not exceed 2MB.");
        }
    }
}

