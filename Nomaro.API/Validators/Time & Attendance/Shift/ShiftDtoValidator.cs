using FluentValidation;
using Nomaro.API.DTO.Shift;

namespace Nomaro.API.Validators.TimeAndAttendance
{
    public class ShiftDtoValidator : AbstractValidator<ShiftDto>
    {
        public ShiftDtoValidator()
        {
            RuleFor(s => s.ShiftName)
                .NotEmpty().WithMessage("Shift name is required.")
                .MaximumLength(100).WithMessage("Shift name must not exceed 100 characters.");
        }
    }
}

