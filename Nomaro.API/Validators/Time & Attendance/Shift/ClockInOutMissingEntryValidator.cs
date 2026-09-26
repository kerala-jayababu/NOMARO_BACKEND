using FluentValidation;
using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Validators.Time___Attendance.Shift
{
    public class UpdateClockInOutMissingEntryValidator : AbstractValidator<UpdateClockInOutMissingEntryDto>
    {
        public UpdateClockInOutMissingEntryValidator()
        {
            RuleFor(x => x.IdClockDetail)
                .GreaterThan(0).WithMessage("IdClockDetail is required.");

            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("IdEmployee is required.");

            RuleFor(x => x.ClockType)
                .NotEmpty().WithMessage("ClockType is required.")
                .Must(type => type == "IN" || type == "OUT")
                .WithMessage("ClockType must be either IN or OUT.");

            RuleFor(x => x.Time)
                .NotEqual(default(DateTime)).WithMessage("Time is required.");

            RuleFor(x => x.Reason)
                .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
        }
    }

}

