using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
{
    public class RentFreeQuarterValidator : AbstractValidator<RentFreeQuarterDto>
    {
        public RentFreeQuarterValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0).WithMessage("Employee ID must be greater than 0.");
            RuleFor(x => x.TotalAnnualRent).GreaterThan(0).WithMessage("Total annual rent must be greater than 0.");
            RuleFor(x => x.DurationInMonths).GreaterThan(0).WithMessage("Duration in months must be greater than 0.");
            RuleFor(x => x.MonthlyRent).GreaterThan(0).WithMessage("Monthly rent must be greater than 0.");
            RuleFor(x => x.TaxRate).InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0 and 100.");
            RuleFor(x => x.AnnualTaxAmount).GreaterThanOrEqualTo(0).WithMessage("Annual tax amount must be greater than or equal to 0.");
            RuleFor(x => x.MonthlyTaxAmount).GreaterThanOrEqualTo(0).WithMessage("Monthly tax amount must be greater than or equal to 0.");
            RuleFor(x => x.ValidFrom).NotEmpty().WithMessage("ValidFrom date is required.");
        }
    }
}
