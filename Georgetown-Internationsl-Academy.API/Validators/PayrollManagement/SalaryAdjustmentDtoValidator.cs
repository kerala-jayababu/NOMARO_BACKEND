using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
{

    public class SalaryAdjustmentDtoValidator : AbstractValidator<SalaryAdjustmentDto>
    {
        public SalaryAdjustmentDtoValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0).WithMessage("Employee ID must be greater than 0.");
            RuleFor(x => x.PayAdjustmentDate).NotEmpty().WithMessage("Pay adjustment date is required.");
            RuleFor(x => x.PayAdjustmentDetails)
                .NotEmpty().WithMessage("Pay adjustment details are required.")
                .MaximumLength(50).WithMessage("Pay adjustment details must not exceed 50 characters.");
            RuleFor(x => x.AllocatingSalaryHead).GreaterThan(0).WithMessage("Allocating salary head must be greater than 0.");
            RuleFor(x => x.EarningOrDeduction)
                .Must(x => x == 'E' || x == 'D')
                .WithMessage("Earning or Deduction must be 'E' or 'D'.");
            RuleFor(x => x.AllocatingSalaryMonth)
      .InclusiveBetween(1, 12)
      .WithMessage("Allocating salary month must be a valid month (1 = January, 2 = February, ..., 12 = December).");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
            RuleFor(x => x.Remarks).MaximumLength(10).WithMessage("Remarks must not exceed 10 characters.");
        }
    }

}
