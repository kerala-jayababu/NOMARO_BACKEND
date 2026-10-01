using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.EmployeeSalaryConfig
{
    public class EmployeeSalaryConfigDetailsValidator : AbstractValidator<EmployeeSalaryConfigDetailsDto>
    {
        public EmployeeSalaryConfigDetailsValidator()
        {
            RuleFor(x => x.IdSalaryHead)
                .GreaterThan(0)
                .WithMessage("Salary Head ID is required.");

            RuleFor(x => x.FixedAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.FixedAmount.HasValue)
                .WithMessage("Enter an amount of 0 or more.");

            RuleFor(x => x.PercentageValue)
                .InclusiveBetween(0.01m, 100m)
                .When(x => x.PercentageValue.HasValue)
                .WithMessage("Enter a percentage between 0.01 and 100.");

            // CalculationMethod, PercentageOfIdSalaryHead and CustomFormula now come from SalaryHeads,
            // and SalaryAmount is calculated by the API.
        }
    }
}
