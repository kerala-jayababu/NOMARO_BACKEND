using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
{
    public class ScheduledSalaryDeductionValidator : AbstractValidator<ScheduledSalaryDeductionDto>
    {
        public ScheduledSalaryDeductionValidator()
        {
            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("Employee ID must be greater than 0.");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than 0.");

            RuleFor(x => x.DeductionFromSalaryMonth)
                .InclusiveBetween(1, 12).WithMessage("Deduction from salary month must be a valid month (1-12).");

            RuleFor(x => x.DeductionToSalaryMonth)
                .InclusiveBetween(1, 12).WithMessage("Deduction to salary month must be a valid month (1-12).")
                .GreaterThanOrEqualTo(x => x.DeductionFromSalaryMonth)
                .WithMessage("Deduction to salary month must be greater than or equal to the deduction from salary month.");

            RuleFor(x => x.AllocatingSalaryHead)
                .GreaterThan(0).WithMessage("Allocating salary head must be greater than 0.");

            RuleFor(x => x.MonthCount)
                .GreaterThan(0).WithMessage("Month count must be greater than 0.");

            RuleFor(x => x.MonthlyDeductableAmount)
                .GreaterThan(0).WithMessage("Monthly deductible amount must be greater than 0.")
                .Equal(x => x.TotalAmount / x.MonthCount)
                .WithMessage("Monthly deductible amount must equal Total Amount divided by Month Count.");
        }
    }
}
