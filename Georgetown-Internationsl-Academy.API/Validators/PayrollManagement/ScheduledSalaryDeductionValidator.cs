using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using System.Text.RegularExpressions;

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
            RuleFor(x => x.TotalAmount)
              .GreaterThan(0) // Ensures the value is greater than zero (optional, based on your needs)
              .ScalePrecision(2, 12) // Ensures up to 12 digits before decimal and 2 after
              .WithMessage("TotalAmount must have up to 12 digits before the decimal and up to 2 decimal places.");




            RuleFor(x => x.DeductionFromSalaryMonth)
                          .GreaterThan(0).WithMessage("DeductionFromSalaryMonth  must be greater than 0.");

            RuleFor(x => x.DeductionToSalaryMonth)              
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

            RuleFor(x => x.DeductionFromSalaryMonthDate)
                .NotEmpty().WithMessage("DeductionFromSalaryMonthDate is required");

            RuleFor(x => x.DeductionToSalaryMonthDate)
                .NotEmpty().WithMessage("DeductionToSalaryMonthDate is required");
        }
    }
}