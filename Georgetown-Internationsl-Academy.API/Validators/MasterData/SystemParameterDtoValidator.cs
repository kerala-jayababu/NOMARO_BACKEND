using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.MasterData
{
    public class SystemParameterDtoValidator : AbstractValidator<SystemParameterDto>
    {
        public SystemParameterDtoValidator()
        {
            RuleFor(x => x.IdSystemParameter)
                .GreaterThan(0).WithMessage("IdSystemParameter must be greater than 0."); // Required field

            RuleFor(x => x.ParameterName)
                .NotEmpty().WithMessage("ParameterName is required.")
                .MaximumLength(50).WithMessage("ParameterName must not exceed 50 characters."); // Required field

            RuleFor(x => x.ParameterDescription)
                .MaximumLength(50).WithMessage("ParameterDescription must not exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.ParameterDescription)); // Validate only if not null or empty

            RuleFor(x => x.ParameterValue)
                .MaximumLength(500).WithMessage("ParameterValue must not exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.ParameterValue)); // Validate only if not null or empty

            RuleFor(x => x.ParameterBinaryValue)
                .Must(value => value == null || value.Length <= 1048576) // Example: 1 MB size limit
                .WithMessage("ParameterBinaryValue must not exceed 1 MB in size."); // Allow null or validate size

            RuleFor(x => x.DataType)
                .MaximumLength(50).WithMessage("DataType must not exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.DataType)); // Validate only if not null or empty

            RuleFor(x => x.ValidValues)
                .MaximumLength(200).WithMessage("ValidValues must not exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.ValidValues)); // Validate only if not null or empty
        }
    }
}
