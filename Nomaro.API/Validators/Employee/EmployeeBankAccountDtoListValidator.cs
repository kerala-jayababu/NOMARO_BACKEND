using FluentValidation;
using Nomaro.API.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nomaro.API.Validators.Employee
{
    public class EmployeeBankAccountDtoListValidator
        : AbstractValidator<List<EmployeeBankAccountDtoList>>
    {
        public EmployeeBankAccountDtoListValidator()
        {
            // 1) Per‐item rules:
            RuleForEach(x => x).ChildRules(account =>
            {
                // IdBank (required)
                account.RuleFor(a => a.IdBank)
                    .NotEmpty()
                    .WithMessage("IdBank is required.");

                // IdBankBranch (required)
                account.RuleFor(a => a.IdBankBranch)
                    .NotEmpty()
                    .WithMessage("IdBankBranch is required.");

                // DisbursementType (required, either "PERCENTAGE" or "FIXEDAMOUNT")
                account.RuleFor(a => a.DisbursementType)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("DisbursementType is required.")
                    .Must(type =>
                    {
                        if (type is null) return false;
                        var trimmed = type.Trim().ToUpperInvariant();
                        return trimmed == "PERCENTAGE" || trimmed == "FIXEDAMOUNT";
                    })
                        .WithMessage("DisbursementType must be either 'PERCENTAGE' or 'FIXEDAMOUNT'.");

                // SalaryPercentageDistributed (only when DisbursementType == "PERCENTAGE")
                account.When(
                    a => string.Equals(a.DisbursementType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase),
                    () =>
                    {
                        account.RuleFor(a => a.SalaryPercentageDistributed)
                            .GreaterThan(0m)
                                .WithMessage("SalaryPercentageDistributed must be greater than 0 when DisbursementType is 'PERCENTAGE'.")
                            .LessThanOrEqualTo(100m)
                                .WithMessage("SalaryPercentageDistributed cannot exceed 100 when DisbursementType is 'PERCENTAGE'.");
                    }
                );

                // AccountNumber (required, max length 30)
                account.RuleFor(a => a.AccountNumber)
                    .NotEmpty()
                        .WithMessage("AccountNumber is required.")
                    .MaximumLength(30)
                        .WithMessage("AccountNumber must not exceed 30 characters.");

                // CurrencyCode (required, max length 10, must be "INR", "GYD" or "USD")
                account.RuleFor(a => a.CurrencyCode)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("CurrencyCode is required.")
                    .MaximumLength(10)
                        .WithMessage("CurrencyCode must not exceed 10 characters.")
                    .Must(code =>
                    {
                        if (string.IsNullOrWhiteSpace(code)) return false;
                        var c = code.Trim().ToUpperInvariant();
                        return c == "INR" || c == "GYD" || c == "USD";
                    })
                        .WithMessage("CurrencyCode must be 'INR', 'GYD' or 'USD'.");
            });

            // 2) “Sum of all SalaryPercentageDistributed ≤ 100” 
            //    only if at least one account is using PERCENTAGE.
            RuleFor(list => list.Sum(a =>
                    string.Equals(a.DisbursementType.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase)
                        ? a.SalaryPercentageDistributed
                        : 0m
                ))
                .LessThanOrEqualTo(100m)
                .When(list => list.Any(a =>
                    string.Equals(a.DisbursementType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase)
                ))
                .WithMessage("The total SalaryPercentageDistributed (for accounts with DisbursementType = 'PERCENTAGE') must be less than or equal to 100.");

            // 3) “At least two bank accounts” → only when EVERY account is FIXEDAMOUNT
            RuleFor(list => list)
                .Must(list => list != null && list.Count >= 2)
                .When(list => list != null
                    && list.Count > 0
                    && list.All(a => string.Equals(a.DisbursementType?.Trim(), "FIXEDAMOUNT", StringComparison.OrdinalIgnoreCase))
                )
                .WithMessage("At least two bank accounts must be provided when all DisbursementType values are 'FIXEDAMOUNT'.");

            // 4) Ensure no duplicate (IdBank, IdBankBranch) combinations:
            RuleFor(list => list)
                .Must(accounts =>
                {
                    if (accounts == null) return true;
                    var duplicates = accounts
                        .GroupBy(a => new { a.IdBank, a.IdBankBranch })
                        .Where(g => g.Count() > 1)
                        .ToList();
                    return !duplicates.Any();
                })
                .WithMessage("Duplicate IdBank and IdBankBranch combination found.");
        }
    }
}

