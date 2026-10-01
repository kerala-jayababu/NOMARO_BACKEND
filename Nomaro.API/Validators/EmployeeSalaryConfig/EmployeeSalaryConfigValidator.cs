using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;

namespace Nomaro.API.Validators.EmployeeSalaryConfig
{
    public class EmployeeSalaryConfigValidator : AbstractValidator<EmployeeSalaryConfigDto>
    {
        public EmployeeSalaryConfigValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0).WithMessage("Select an employee.");

            RuleFor(x => x.ValidFrom)
                .NotEmpty().WithMessage("ValidFrom is required.")
                .Must(d => !d.HasValue || d.Value.Day == 1).WithMessage("Valid From must be the first day of a month.");

            RuleFor(x => x.RevisionReason)
                .Must(r => SalaryHeadConstants.RevisionReasons.Contains(r!.Trim().ToUpper()))
                .WithMessage("Revision Reason must be one of the following: JOINING, INCREMENT, PROMOTION, CORRECTION.")
                .When(x => !string.IsNullOrWhiteSpace(x.RevisionReason));

            RuleFor(x => x.EmployeeSalaryConfigDetails)
                .NotEmpty().WithMessage("Add at least one earning.");

            // Totals are recalculated by the API, so they are not validated here.
        }
    }
}
