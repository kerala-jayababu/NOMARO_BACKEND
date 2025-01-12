using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.MasterData
{
    public class BudgetCodeDtoValidator : AbstractValidator<BudgetCodeDto>
    {
        public BudgetCodeDtoValidator()
        {
            RuleFor(b => b.IdBudgetCode)
          .NotEmpty()
          .When(dto => dto.IdBudgetCode.HasValue)
          .WithMessage("Id is required for updates.");

            RuleFor(b => b.BudgetCode)
                .NotEmpty().WithMessage("BudgetCode is required.")
                .MaximumLength(10).WithMessage("BudgetCode must not exceed 10 characters.")
                .Matches("^[a-zA-Z0-9]*$").WithMessage("BudgetCode must contain only alphanumeric characters.");

            RuleFor(b => b.BudgetCodeName)
                .NotEmpty().WithMessage("BudgetCodeName is required.")
                .MaximumLength(50).WithMessage("BudgetCodeName must not exceed 50 characters.")
                .Matches("^[a-zA-Z0-9]*$").WithMessage("BudgetCodeName must contain only alphanumeric characters.");
        }
    }
}
