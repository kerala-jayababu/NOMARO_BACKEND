using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.MasterData
{
    public class DesignationDtoValidator : AbstractValidator<DesignationDto>
    {
        public DesignationDtoValidator()
        {
            RuleFor(b => b.IdDesignation)
       .NotEmpty()
       .When(dto => dto.IdDesignation.HasValue)
       .WithMessage("Id is required for updates.");

            RuleFor(d => d.DesignationCode)
                .NotEmpty().WithMessage("Designation code is required.")
                .MaximumLength(10).WithMessage("Designation code must not exceed 10 characters.");

            RuleFor(d => d.DesignationName)
                .NotEmpty().WithMessage("Designation name is required.")
                .MaximumLength(50).WithMessage("Designation name must not exceed 50 characters.");

            RuleFor(d => d.IsOvertimeAllowanceAllowed)
                .NotNull().WithMessage("Overtime allowance must be specified.");
        }
    }

}
