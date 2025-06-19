using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;

namespace Georgetown_Internationsl_Academy.API.Validators.Time___Attendance.Shift
{
    public class UpdateShortTimeReasonDtoValidator : AbstractValidator<UpdateShortTimeReasonDto>
    {
        public UpdateShortTimeReasonDtoValidator()
        {
            RuleFor(x => x.IdDayAttendance)
                .GreaterThan(0).WithMessage("IdDayAttendance is required.");

            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("IdEmployee is required.");

            RuleFor(x => x.ReasonForShortTime)
                .NotEmpty().WithMessage("Reason for short time is required.")
                .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
        }
    }
}
