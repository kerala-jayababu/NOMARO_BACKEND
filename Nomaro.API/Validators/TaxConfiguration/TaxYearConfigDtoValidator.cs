using FluentValidation;
using Nomaro.API.DTO;

public class TaxYearConfigDtoValidator : AbstractValidator<TaxYearConfigDto>
{
    public TaxYearConfigDtoValidator()
    {
        RuleFor(x => x.IdFinancialYear)
            .GreaterThan(0).WithMessage("Select the financial year.");

        RuleFor(x => x.IdTaxRegime)
            .GreaterThan(0).WithMessage("Select the tax regime.");

        RuleFor(x => x.StandardDeduction)
            .GreaterThanOrEqualTo(0).WithMessage("Standard Deduction must be 0 or more.");

        RuleFor(x => x.RebateIncomeLimit)
            .GreaterThanOrEqualTo(0).When(x => x.RebateIncomeLimit.HasValue)
            .WithMessage("Rebate Income Limit must be 0 or more.");

        RuleFor(x => x.RebateMaxAmount)
            .GreaterThanOrEqualTo(0).When(x => x.RebateMaxAmount.HasValue)
            .WithMessage("Rebate Max Amount must be 0 or more.");

        RuleFor(x => x)
            .Must(x => x.RebateIncomeLimit.HasValue == x.RebateMaxAmount.HasValue)
            .WithMessage("Enter both Rebate Income Limit and Rebate Max Amount, or leave both empty.");

        RuleFor(x => x.CessRate)
            .InclusiveBetween(0, 100).WithMessage("Cess Rate must be between 0 and 100.");
    }
}
