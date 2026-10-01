using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.Employee
{
    public class EmployeeStatutoryDetailsDtoValidator : AbstractValidator<EmployeeStatutoryDetailsDto>
    {
        public EmployeeStatutoryDetailsDtoValidator()
        {
            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("Select an employee.");

            RuleFor(x => x.PAN)
                .Matches("^[A-Z]{5}[0-9]{4}[A-Z]$").WithMessage("PAN must be 10 characters, like ABCDE1234F.")
                .When(x => !string.IsNullOrWhiteSpace(x.PAN));

            RuleFor(x => x.UAN)
                .Matches("^[0-9]{12}$").WithMessage("UAN must be 12 digits.")
                .When(x => !string.IsNullOrWhiteSpace(x.UAN));

            RuleFor(x => x.PFNumber)
                .MaximumLength(30).WithMessage("PF Number must not exceed 30 characters.");

            RuleFor(x => x.ESINumber)
                .Matches("^([0-9]{10}|[0-9]{17})$").WithMessage("ESI Number must be 10 or 17 digits.")
                .When(x => !string.IsNullOrWhiteSpace(x.ESINumber));

            RuleFor(x => x.VPFRate)
                .InclusiveBetween(0, 100).WithMessage("VPF Rate must be between 0 and 100.")
                .When(x => x.VPFRate.HasValue);

            RuleFor(x => x.IdPTState)
                .GreaterThan(0).WithMessage("Select the PT state.")
                .When(x => x.IdPTState.HasValue);
        }
    }
}
