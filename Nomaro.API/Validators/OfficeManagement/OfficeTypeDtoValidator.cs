using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.OfficeManagement
{
    public class OfficeTypeDtoValidator : AbstractValidator<OfficeTypeDto>
    {
        public OfficeTypeDtoValidator()
        {
            RuleFor(x => x.OfficeTypeCode)
                .NotEmpty().WithMessage("Office type code is required.")
                .MaximumLength(20).WithMessage("Office type code must not exceed 20 characters.");

            RuleFor(x => x.OfficeTypeName)
                .NotEmpty().WithMessage("Office type name is required.")
                .MaximumLength(100).WithMessage("Office type name must not exceed 100 characters.");

            RuleFor(x => x.HierarchyLevel)
                .GreaterThan(0).WithMessage("Hierarchy level must be greater than 0.");
        }
    }
}
