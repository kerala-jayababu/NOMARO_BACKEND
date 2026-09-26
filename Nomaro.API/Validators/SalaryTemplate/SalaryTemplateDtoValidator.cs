using FluentValidation;
using Nomaro.API.DTO;

public class SalaryTemplateDtoValidator : AbstractValidator<SalaryTemplateManageDto>
{
    public SalaryTemplateDtoValidator()
    {
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

        RuleFor(x => x.TotalEarnings).GreaterThan(0).WithMessage("TotalEarnings is required.");
        RuleFor(x => x.TotalDeductions).GreaterThan(0).WithMessage("TotalDeductions is required.");
        RuleFor(x => x.NetSalary).GreaterThan(0).WithMessage("NetSalary is required.");


    }
}

