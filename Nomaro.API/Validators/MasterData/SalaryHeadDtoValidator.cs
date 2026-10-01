using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;

namespace Nomaro.API.Validators.MasterData
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
                .MaximumLength(50).WithMessage("SalaryHeadCode must not exceed 50 characters.")
                .Matches(@"^[A-Z0-9_]+$").WithMessage("SalaryHeadCode may contain only capital letters, digits and _.");

            RuleFor(s => s.CalcSequence)
                .NotNull().WithMessage("Calculation Sequence is required.");

            // Percentage: Percentage and Percentage Of are both required
            RuleFor(s => s.PercentageValue)
                .NotNull().WithMessage("Percentage is required for a Percentage head.")
                .When(s => string.Equals(s.CalculationMethod, "PERCENTAGE", StringComparison.OrdinalIgnoreCase));
            RuleFor(s => s.IdPercentageSalaryHead)
                .NotNull().WithMessage("Percentage Of is required for a Percentage head.")
                .GreaterThan(0).WithMessage("Percentage Of is required for a Percentage head.")
                .When(s => string.Equals(s.CalculationMethod, "PERCENTAGE", StringComparison.OrdinalIgnoreCase));

            // Formula: allow only [CODE], numbers, + - * / ( ) and spaces
            RuleFor(s => s.CustomFormula)
                .NotEmpty().WithMessage("Formula is required for a Formula head.")
                .Must(f => SalaryHeadConstants.AllowedFormula.IsMatch(f!)).WithMessage("Formula may contain only [CODE], numbers, + - * / ( ) and spaces.")
                .When(s => string.Equals(s.CalculationMethod, "FORMULA", StringComparison.OrdinalIgnoreCase));

            RuleFor(s => s.SalaryHeadName)
                .NotEmpty().WithMessage("SalaryHeadName is required.")
                .MaximumLength(60).WithMessage("SalaryHeadName must not exceed 60 characters.");


            RuleFor(s => s.HeadType)
     .NotEmpty().WithMessage("HeadType is required.")
     .MaximumLength(50).WithMessage("HeadType must not exceed 50 characters.")
     .Must(BeAValidHeadType)
     .WithMessage("HeadType must be one of the following: EARNINGS, DEDUCTION, REIMBURSEMENT, EMPLOYER_CONTRIBUTION.");

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
         .WithMessage("CalculationMethod must be one of the following: FORMULA, PERCENTAGE, FIXEDAMOUNT, MANUAL, STATUTORY.");
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

            RuleFor(s => s.OrderNumber)
                .NotNull().WithMessage("OrderNumber is required.");

            RuleFor(s => s.DisbursingMonths)
               .Matches(@"^([A-Z]+)(,([A-Z]+))*$").WithMessage("DisbursingMonths must be a comma-separated list of uppercase month names.")
               .Must(BeValidMonths).WithMessage("DisbursingMonths must contain valid month names (e.g., JANUARY, FEBRUARY, etc.)")
               .When(s => !string.IsNullOrEmpty(s.DisbursingMonths));

            RuleFor(s => s.StatutoryType)
                .NotEmpty().WithMessage("StatutoryType is required when CalculationMethod is STATUTORY.")
                .Must(t => SalaryHeadConstants.StatutoryTypes.Contains(t!.Trim().ToUpper()))
                .WithMessage("StatutoryType must be one of the following: " + string.Join(", ", SalaryHeadConstants.StatutoryTypes) + ".")
                .When(s => string.Equals(s.CalculationMethod, "STATUTORY", StringComparison.OrdinalIgnoreCase));

            RuleFor(s => s.StatutoryType)
                .MaximumLength(20).WithMessage("StatutoryType must not exceed 20 characters.")
                .When(s => !string.IsNullOrEmpty(s.StatutoryType));

            RuleFor(s => s.IdBaseSalaryHead)
                .NotNull().WithMessage("IdBaseSalaryHead is required for an arrear head.")
                .GreaterThan(0).WithMessage("IdBaseSalaryHead must be greater than 0.")
                .When(s => s.IsArrearHead);

            RuleFor(s => s.IdBaseSalaryHead)
                .NotEqual(s => s.IdSalaryHead).WithMessage("A salary head cannot be its own base salary head.")
                .When(s => s.IdBaseSalaryHead.HasValue && s.IdSalaryHead.HasValue && s.IdSalaryHead > 0);

            RuleFor(s => s.MinAmount)
                .GreaterThanOrEqualTo(0).WithMessage("MinAmount must be greater than or equal to 0.")
                .When(s => s.MinAmount.HasValue);

            RuleFor(s => s.MaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("MaxAmount must be greater than or equal to 0.")
                .When(s => s.MaxAmount.HasValue);

            RuleFor(s => s.MaxAmount)
                .GreaterThanOrEqualTo(s => s.MinAmount).WithMessage("MaxAmount must be greater than or equal to MinAmount.")
                .When(s => s.MinAmount.HasValue && s.MaxAmount.HasValue);

            RuleFor(s => s.WageCeiling)
                .GreaterThanOrEqualTo(0).WithMessage("WageCeiling must be greater than or equal to 0.")
                .When(s => s.WageCeiling.HasValue);

            RuleFor(s => s.PayFrequency)
                .Must(BeAValidPayFrequency)
                .WithMessage("PayFrequency must be one of the following: MONTHLY, QUARTERLY, HALF_YEARLY, ANNUAL, ONE_TIME.")
                .When(s => !string.IsNullOrEmpty(s.PayFrequency));

            RuleFor(s => s.CalcSequence)
                .GreaterThanOrEqualTo(0).WithMessage("CalcSequence must be greater than or equal to 0.")
                .When(s => s.CalcSequence.HasValue);

            RuleFor(s => s.RoundingRule)
                .Must(r => SalaryHeadConstants.RoundingRules.Contains(r!.Trim().ToUpper()))
                .WithMessage("RoundingRule must be one of the following: NEAREST, UP, DOWN, NONE.")
                .When(s => !string.IsNullOrEmpty(s.RoundingRule));
        }

        private bool BeAValidCalculationMethod(string calculationMethod)
        {
            var validCalculationMethods = new[] { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT", "MANUAL", "STATUTORY" };
            return !string.IsNullOrWhiteSpace(calculationMethod) &&
                   validCalculationMethods.Contains(calculationMethod.ToUpper());
        }
        private bool BeAValidHeadType(string headType)
        {
            var validHeadTypes = new[] { "EARNINGS", "DEDUCTION", "REIMBURSEMENT", "EMPLOYER_CONTRIBUTION" };
            return !string.IsNullOrWhiteSpace(headType) &&
                   validHeadTypes.Contains(headType.Trim().ToUpper());
        }
        private bool BeAValidPayFrequency(string? payFrequency)
        {
            var validPayFrequencies = SalaryHeadConstants.PayFrequencies;
            return !string.IsNullOrWhiteSpace(payFrequency) &&
                   validPayFrequencies.Contains(payFrequency.Trim().ToUpper());
        }
        private bool BeValidMonths(string disbursingMonths)
        {
            if (string.IsNullOrWhiteSpace(disbursingMonths)) return true;

            var validMonths = new[] {
                "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER",
                "OCTOBER", "NOVEMBER", "DECEMBER"
            };

            var monthsList = disbursingMonths.Split(',')
                                              .Select(m => m.Trim())
                                              .Distinct()
                                              .ToList();

            return monthsList.All(m => validMonths.Contains(m));
        }
    }

   

}

