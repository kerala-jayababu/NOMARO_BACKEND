using FluentValidation;
using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Validators.Time___Attendance.Shift
{
    public class ApproveTimesheetDtoValidator : AbstractValidator<ApproveTimesheetDto>
    {
        public ApproveTimesheetDtoValidator()
        {
            RuleFor(x => x.IdDayAttendance)
                .GreaterThan(0).WithMessage("IdDayAttendance is required.");

            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("IdEmployee is required.");

            RuleFor(x => x.ApprovalStatus)
                .NotEmpty().WithMessage("ApprovalStatus is required.")
                .Must(status => status == "APPROVED" || status == "REJECTED")
                .WithMessage("ApprovalStatus must be either APPROVED or REJECTED.");

            RuleFor(x => x.RejectReasons)
                .MaximumLength(200).WithMessage("RejectReasons cannot exceed 200 characters.");
        }
    }
}

