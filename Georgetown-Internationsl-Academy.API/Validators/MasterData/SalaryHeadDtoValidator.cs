using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.MasterData
{
    public class SalaryHeadDtoValidator : AbstractValidator<SalaryHeadDto>
    {
        public SalaryHeadDtoValidator()
        {
        

            RuleFor(b => b.IdSalaryHead)
       .NotEmpty()
       .When(dto => dto.IdSalaryHead.HasValue)
       .WithMessage("IdSalaryHead is required for updates.");

            RuleFor(s => s.SalaryHeadCode)
                .NotEmpty().WithMessage("SalaryHeadCode is required.")
                .MaximumLength(50).WithMessage("SalaryHeadCode must not exceed 50 characters.");

            RuleFor(s => s.SalaryHeadName)
                .NotEmpty().WithMessage("SalaryHeadName is required.")
                .MaximumLength(60).WithMessage("SalaryHeadName must not exceed 60 characters.");

            
            RuleFor(s => s.HeadType)
              .NotEmpty().WithMessage("HeadType is required.")
              .MaximumLength(50).WithMessage("HeadType must not exceed 50 characters.")
              .Must(BeAValidHeadType).WithMessage("HeadType must be either 'Earning' or 'Deduction'.");

            RuleFor(s => s.IsTaxable)
                .NotNull().WithMessage("IsTaxable is required.");

            RuleFor(s => s.IsActive)
                .NotNull().WithMessage("IsActive is required.");

            RuleFor(s => s.CalculationMethod)
         .NotEmpty()
         .WithMessage("CalculationMethod is required.")
         .MaximumLength(50)
         .WithMessage("CalculationMethod must not exceed 50 characters.")
         .Must(BeAValidCalculationMethod)
         .WithMessage("CalculationMethod must be one of the following: FORMULA, PERCENTAGE, FIXEDAMOUNT.");
            // Validation for IdPercentageSalaryHead
            RuleFor(s => s.IdPercentageSalaryHead)
                .GreaterThanOrEqualTo(0).WithMessage("IdPercentageSalaryHead must be greater than or equal to 0.")
                .When(s => s.IdPercentageSalaryHead.HasValue);

            RuleFor(s => s.PercentageValue)
                .InclusiveBetween(0, 100).WithMessage("PercentageValue must be between 0 and 100.")
                .When(s => s.PercentageValue.HasValue);

            RuleFor(s => s.FixedValue)
                .GreaterThanOrEqualTo(0).WithMessage("FixedValue must be greater than or equal to 0.")
                .When(s => s.FixedValue.HasValue);

            RuleFor(s => s.CustomFormula)
                .MaximumLength(100).WithMessage("CustomFormula must not exceed 100 characters.")
                .When(s => !string.IsNullOrEmpty(s.CustomFormula));
          

            RuleFor(s => s.IsLOPSalaryHead)
                .NotNull().WithMessage("IsLOPSalaryHead is required.");
        }

        private bool BeAValidCalculationMethod(string calculationMethod)
        {
            var validCalculationMethods = new[] { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT" };
            return !string.IsNullOrWhiteSpace(calculationMethod) &&
                   validCalculationMethods.Contains(calculationMethod.ToUpper());
        }
        private bool BeAValidHeadType(string headType)
        {
            var validHeadTypes = new[] { "Earning", "Deduction" };
            return !string.IsNullOrWhiteSpace(headType) &&
                   validHeadTypes.Contains(headType);
        }
    }

   

}
