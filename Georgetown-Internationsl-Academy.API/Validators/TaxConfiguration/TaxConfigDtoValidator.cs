using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

public class TaxConfigDtoValidator : AbstractValidator<TaxConfigManageDto>
{
    public TaxConfigDtoValidator()
    {
        
        

        // FinancialYearDesc (Required and max length 50)
        RuleFor(x => x.FinancialYearDesc)
            .NotEmpty().WithMessage("FinancialYearDesc is required.")
            .MaximumLength(50).WithMessage("FinancialYearDesc must not exceed 50 characters.");

        // ValidFrom (Required)
        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage("ValidFrom is required.")
            .LessThanOrEqualTo(x => x.ValidTo).WithMessage("ValidFrom must be earlier than or equal to ValidTo.");

        // ValidTo (Required)
        RuleFor(x => x.ValidTo)
            .NotEmpty().WithMessage("ValidTo is required.");

    
    }
}
