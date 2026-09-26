using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.MasterData
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
                .MaximumLength(10).WithMessage("BudgetCode must not exceed 10 characters.");

            RuleFor(b => b.BudgetCodeName)
                .NotEmpty().WithMessage("BudgetCodeName is required.")
                .MaximumLength(50).WithMessage("BudgetCodeName must not exceed 50 characters.");
        }
    }
}

