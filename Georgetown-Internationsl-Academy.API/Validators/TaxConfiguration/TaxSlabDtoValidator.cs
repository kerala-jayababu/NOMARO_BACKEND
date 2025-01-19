using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

public class TaxSlabDtoValidator : AbstractValidator<TaxSlabDto>
{
    public TaxSlabDtoValidator()
    {
        RuleFor(x => x.IdTaxConfig)
            .GreaterThan(0).WithMessage("IdTaxConfig must be greater than 0.");

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0).WithMessage("MinAmount must be greater than or equal to 0.");

        RuleFor(x => x.MaxAmount)
            .GreaterThan(x => x.MinAmount)
            .WithMessage("MaxAmount must be greater than MinAmount.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100).WithMessage("TaxRate must be between 0 and 100.");
    }
}
