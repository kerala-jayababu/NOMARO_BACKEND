using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.MasterData
{
    public class GradeDtoValidator : AbstractValidator<GradeDto>
    {
        public GradeDtoValidator()
        {
            RuleFor(x => x.GradeCode)
                .NotEmpty().WithMessage("Grade code is required.")
                .MaximumLength(10).WithMessage("Grade code must not exceed 10 characters.")
                .Matches("^[A-Za-z0-9]+$").WithMessage("Grade code must be alphanumeric.");

            RuleFor(x => x.GradeName)
                .NotEmpty().WithMessage("Level name is required.")
                .MaximumLength(100).WithMessage("Level name must not exceed 100 characters.");

            RuleFor(x => x.GradeLevel)
                .InclusiveBetween(1, 999).WithMessage("Grade level must be between 1 and 999.");

            RuleFor(x => x.Description)
                .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
        }
    }
}
