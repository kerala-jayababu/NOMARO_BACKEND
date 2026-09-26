using FluentValidation;
using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Validators.Time___Attendance.Shift
{
    public class ShiftScheduleDtoValidator : AbstractValidator<ShiftScheduleDto>
    {
        private readonly string[] validDays = new[] { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };

        public ShiftScheduleDtoValidator()
        {
            RuleFor(x => x.IdShift).GreaterThan(0).WithMessage("Shift ID is required.");

            RuleFor(x => x.StartTime).NotEmpty().WithMessage("Start time is required.");
            RuleFor(x => x.EndTime).NotEmpty().WithMessage("End time is required.");

            RuleFor(x => x.WorkDays)
                .NotEmpty().WithMessage("WorkDays is required.")
                .Must(BeValidWorkDays).WithMessage("WorkDays must only contain days from SUNDAY to SATURDAY in uppercase.");
        }

        private bool BeValidWorkDays(string workDays)
        {
            var inputDays = workDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(d => d.Trim().ToUpper());
            return inputDays.All(d => validDays.Contains(d));
        }
    }

}

