using FluentValidation;
using Nomaro.API.DTO;

public class TaxSlabDtoValidator : AbstractValidator<TaxSlabDto>
{
    public TaxSlabDtoValidator()
    {
        RuleFor(x => x.IdTaxYearConfig)
            .GreaterThan(0).WithMessage("Select the financial year and tax regime.");

        RuleFor(x => x.AgeCategory)
            .NotEmpty().WithMessage("Age Category is required.")
            .MaximumLength(20).WithMessage("Age Category must not exceed 20 characters.");

        RuleFor(x => x.IncomeFrom)
            .GreaterThanOrEqualTo(0).WithMessage("Income From must be 0 or more.");

        RuleFor(x => x.IncomeTo)
            .GreaterThan(x => x.IncomeFrom)
            .When(x => x.IncomeTo.HasValue)
            .WithMessage("Income To must be greater than Income From (leave it empty for the top slab).");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100).WithMessage("Tax Rate must be between 0 and 100.");
    }
}
