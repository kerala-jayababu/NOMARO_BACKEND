using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.EmployeeSalaryConfig
{
    public class EmployeeSalaryConfigDetailsValidator : AbstractValidator<EmployeeSalaryConfigDetailsDto>
    {
        public EmployeeSalaryConfigDetailsValidator()
        {
            RuleFor(x => x.IdEmployeeSalaryConfig)
                .GreaterThan(0)
                .WithMessage("Employee Salary Config ID is required.");

            RuleFor(x => x.IdSalaryHead)
                .GreaterThan(0)
                .WithMessage("Salary Head ID is required.");

            RuleFor(s => s.CalculationMethod)
                .NotEmpty()
                .WithMessage("CalculationMethod is required.")
                .MaximumLength(50)
                .WithMessage("CalculationMethod must not exceed 50 characters.")
                .Must(BeAValidCalculationMethod)
                .WithMessage("CalculationMethod must be one of the following: FORMULA, PERCENTAGE, FIXEDAMOUNT.");

            RuleFor(x => x.FixedAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.FixedAmount.HasValue)
                .WithMessage("FixedAmount must be a positive number.");

            RuleFor(x => x.PercentageValue)
                .InclusiveBetween(0, 100)
                .When(x => x.PercentageValue.HasValue)
                .WithMessage("PercentageValue must be between 0 and 100.");

            // SalaryAmount: only check it's numeric, no restriction on decimal places
            RuleFor(x => x.SalaryAmount)
                .Must(value => value == null || decimal.TryParse(value.ToString(), out _))
                .WithMessage("SalaryAmount must be a valid number.")
                .When(x => x.SalaryAmount.HasValue);
        }

        private bool BeAValidCalculationMethod(string calculationMethod)
        {
            var validCalculationMethods = new[] { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT" };
            return !string.IsNullOrWhiteSpace(calculationMethod) &&
                   validCalculationMethods.Contains(calculationMethod.ToUpper());
        }
    }
}

