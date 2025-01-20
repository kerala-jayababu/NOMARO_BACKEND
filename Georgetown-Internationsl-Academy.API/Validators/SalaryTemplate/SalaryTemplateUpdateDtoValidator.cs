using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

public class SalaryTemplateUpdateDtoValidator : AbstractValidator<SalaryTemplateDto>
{
    public SalaryTemplateUpdateDtoValidator()
    {
        // IdSalaryTemplate is required and must be greater than 0
        RuleFor(x => x.IdSalaryTemplate)
            .GreaterThan(0).WithMessage("IdSalaryTemplate must be greater than 0.");

        // SalaryTemplateName is required and must not exceed 50 characters
        RuleFor(x => x.SalaryTemplateName)
            .NotEmpty().WithMessage("SalaryTemplateName is required.")
            .MaximumLength(50).WithMessage("SalaryTemplateName must not exceed 50 characters.");

        // Description is optional, but if provided, must not exceed 500 characters
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // ActiveStatus is required
        RuleFor(x => x.ActiveStatus)
            .NotNull().WithMessage("ActiveStatus is required.");

        // ApprovalStatus is optional, but if provided, must not exceed 20 characters
        RuleFor(x => x.ApprovalStatus)
            .MaximumLength(20).WithMessage("ApprovalStatus must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.ApprovalStatus));
    }
}
