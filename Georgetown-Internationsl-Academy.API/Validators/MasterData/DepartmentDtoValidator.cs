using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.MasterData
{
    public class DepartmentDtoValidator : AbstractValidator<DepartmentDto>
    {
        public DepartmentDtoValidator()
        {
            RuleFor(b => b.IdDepartment)
        .NotEmpty()
        .When(dto => dto.IdDepartment.HasValue)
        .WithMessage("Id is required for updates.");

            RuleFor(d => d.DepartmentCode)
                .NotEmpty().WithMessage("Department code is required.")
                .MaximumLength(10).WithMessage("Department code must not exceed 10 characters.")
                .Matches("^[a-zA-Z0-9]*$").WithMessage("Department code must contain only alphanumeric characters.");

            RuleFor(d => d.DepartmentName)
                .NotEmpty().WithMessage("Department name is required.")
                .MaximumLength(50).WithMessage("Department name must not exceed 50 characters.");
        }
    }

}
