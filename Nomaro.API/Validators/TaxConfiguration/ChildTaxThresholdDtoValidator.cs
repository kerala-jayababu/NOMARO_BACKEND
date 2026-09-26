using FluentValidation;
using Nomaro.API.DTO;

public class ChildTaxThresholdDtoValidator : AbstractValidator<ChildTaxThresholdDto>
{
    public ChildTaxThresholdDtoValidator()
    {
        RuleFor(x => x.IdFinancialYear)
             .GreaterThan(0).WithMessage("IdTaxConfig must be greater than 0.");

        RuleFor(x => x.ChildrenCount)
            .GreaterThanOrEqualTo(0).WithMessage("ChildrenCount cannot be negative.");

        RuleFor(x => x.TaxThresholdAmount)
            .GreaterThan(0).WithMessage("TaxThresholdAmount must be greater than 0.");
    }
}

