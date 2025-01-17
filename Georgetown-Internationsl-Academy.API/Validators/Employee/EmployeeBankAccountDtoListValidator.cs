using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.Employee
{
    
    public class EmployeeBankAccountDtoListValidator : AbstractValidator<List<EmployeeBankAccountDtoList>>
    {
        public EmployeeBankAccountDtoListValidator()
        {
            RuleForEach(x => x).ChildRules(account =>
            {
                account.RuleFor(a => a.IdBank)
                    .NotEmpty().WithMessage("IdBank is required.");

                account.RuleFor(a => a.IdBankBranch)
                    .NotEmpty().WithMessage("IdBankBranch is required.");

                account.RuleFor(a => a.SalaryPercentageDistributed)
                    .GreaterThan(0).WithMessage("SalaryPercentageDistributed must be greater than 0.")
                    .LessThanOrEqualTo(100).WithMessage("SalaryPercentageDistributed cannot exceed 100.");

                account.RuleFor(a => a.AccountNumber)
                    .NotEmpty().WithMessage("AccountNumber is required.")
                    .MaximumLength(30).WithMessage("AccountNumber must not exceed 30 characters.");

                account.RuleFor(a => a.CurrencyCode)
     .NotEmpty().WithMessage("CurrencyCode is required.")
     .MaximumLength(10).WithMessage("CurrencyCode must not exceed 10 characters.")
     .Must(code => new[] { "GYD", "USD" }.Contains(code?.ToUpper().Trim()))
     .WithMessage("CurrencyCode must be either 'GYD' or 'USD'.");
            });

            // Ensure SalaryPercentageDistributed sum is 100
            RuleFor(x => x.Sum(a => a.SalaryPercentageDistributed))
    .LessThanOrEqualTo(100).WithMessage("The total SalaryPercentageDistributed must be less than or equal to 100.");


            // Ensure IdBank and IdBankBranch are not duplicate
            RuleFor(x => x)
                .Must(accounts =>
                {
                    var duplicates = accounts.  GroupBy(a => new { a.IdBank, a.IdBankBranch })
                        .Where(g => g.Count() > 1)
                        .ToList();
                    return !duplicates.Any();
                })
                .WithMessage("Duplicate IdBank and IdBankBranch combination found.");
        }
    }
}
