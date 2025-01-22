using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
{
    public class OvertimeTransactionDtoValidator : AbstractValidator<OvertimeTransactionDto>
    {
        public OvertimeTransactionDtoValidator()
        {
            RuleFor(o => o.IdEmployee).
                GreaterThan(0).WithMessage("Employee ID is required.");

            RuleFor(o => o.IdOvertimeType).
               GreaterThan(0).WithMessage("Overtime Type is required.");

            RuleFor(o => o.OvertimeDate)
                .NotEmpty().WithMessage("Overtime date is required.");

            RuleFor(o => o.StartTime)
                .NotEmpty().WithMessage("Start time is required.");

            RuleFor(o => o.DurationInHours)
                .GreaterThan(0).WithMessage("Duration must be greater than 0.");

            RuleFor(o => o.ReasonForOverTime)
                .MaximumLength(200).WithMessage("Reason for overtime must not exceed 200 characters.");
        }
    }
}
