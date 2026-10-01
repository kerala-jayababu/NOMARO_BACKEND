using System.Globalization;
using System.Text.RegularExpressions;
using Nomaro.API.DTO;
using Nomaro.API.Models;

namespace Nomaro.API.Helpers
{
    /// <summary>
    /// Value lists for SalaryHeads columns. Keep in sync with the salary stored procedures.
    /// </summary>
    public static class SalaryHeadConstants
    {
        public const string Earnings = "EARNINGS";
        public const string Deduction = "DEDUCTION";
        public const string Reimbursement = "REIMBURSEMENT";
        public const string EmployerContribution = "EMPLOYER_CONTRIBUTION";
        public static readonly string[] HeadTypes = { Earnings, Deduction, Reimbursement, EmployerContribution };

        public const string FixedAmount = "FIXEDAMOUNT";
        public const string Percentage = "PERCENTAGE";
        public const string Formula = "FORMULA";
        public const string Statutory = "STATUTORY";
        public const string Manual = "MANUAL";
        public static readonly string[] CalculationMethods = { FixedAmount, Percentage, Formula, Statutory, Manual };

        // Employee statutory items are deductions, employer items are employer contributions
        public static readonly string[] EmployeeStatutoryTypes = { "PF_EE", "VPF", "ESI_EE", "PT", "LWF_EE", "TDS" };
        public static readonly string[] EmployerStatutoryTypes = { "PF_ER_EPF", "PF_ER_EPS", "EDLI", "PF_ADMIN", "ESI_ER", "LWF_ER" };
        public static readonly string[] StatutoryTypes = EmployeeStatutoryTypes.Concat(EmployerStatutoryTypes).ToArray();

        public const string Monthly = "MONTHLY";
        public static readonly string[] PayFrequencies = { Monthly, "QUARTERLY", "HALF_YEARLY", "ANNUAL", "ONE_TIME" };

        public static readonly string[] RoundingRules = { "NEAREST", "UP", "DOWN", "NONE" };

        public static readonly string[] RevisionReasons = { "JOINING", "INCREMENT", "PROMOTION", "CORRECTION" };

        public static readonly string[] Months =
        {
            "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER",
            "OCTOBER", "NOVEMBER", "DECEMBER"
        };

        /// <summary>Formula may contain only [CODE], numbers, + - * / ( ) and spaces.</summary>
        public static readonly Regex AllowedFormula = new(@"^(\[[A-Z0-9_]+\]|\d+(\.\d+)?|[+\-*/() ])+$", RegexOptions.Compiled);
        public static readonly Regex FormulaCode = new(@"\[([A-Z0-9_]+)\]", RegexOptions.Compiled);
        public static readonly Regex HeadCode = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);

        public static List<string> GetFormulaCodes(string? formula) =>
            string.IsNullOrWhiteSpace(formula)
                ? new List<string>()
                : FormulaCode.Matches(formula).Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    /// <summary>
    /// Calculates a salary structure (template or employee salary config) the same way the salary procedure does.
    /// The calculation method, base head and formula always come from SalaryHeads; a row only carries the
    /// amount (Fixed Amount heads) or the percentage (Percentage heads).
    /// </summary>
    public static class SalaryStructureCalculator
    {
        public static SalaryStructureResultDto Calculate(IEnumerable<SalaryStructureRowDto> inputRows, IReadOnlyDictionary<int, SalaryHeads> heads)
        {
            var rows = inputRows.ToList();
            var errors = new List<string>();
            var result = new SalaryStructureResultDto();

            // ---------- Row checks ----------
            if (rows.GroupBy(r => r.IdSalaryHead).Any(g => g.Count() > 1))
            {
                errors.Add("This salary head is already in the list.");
            }

            foreach (var row in rows)
            {
                if (!heads.TryGetValue(row.IdSalaryHead, out var head))
                {
                    errors.Add($"Salary head {row.IdSalaryHead} does not exist.");
                    continue;
                }
                if (!head.IsActive || head.CalculationMethod == SalaryHeadConstants.Statutory || head.CalculationMethod == SalaryHeadConstants.Manual)
                {
                    errors.Add($"\"{head.SalaryHeadName}\" cannot be added here. Only active heads that are not Statutory or Manual are allowed.");
                    continue;
                }
                if (head.CalculationMethod == SalaryHeadConstants.FixedAmount && row.FixedAmount < 0)
                {
                    errors.Add("Enter an amount of 0 or more.");
                }
                if (head.CalculationMethod == SalaryHeadConstants.Percentage)
                {
                    var pct = row.PercentageValue ?? head.PercentageValue;
                    if (!pct.HasValue || pct < 0.01m || pct > 100)
                    {
                        errors.Add("Enter a percentage between 0.01 and 100.");
                    }
                    if (head.IdPercentageSalaryHead.HasValue && !rows.Any(r => r.IdSalaryHead == head.IdPercentageSalaryHead))
                    {
                        var baseName = heads.TryGetValue(head.IdPercentageSalaryHead.Value, out var baseHead) ? baseHead.SalaryHeadName : "the base head";
                        errors.Add($"Add \"{baseName}\" first: {head.SalaryHeadName} is a percentage of it.");
                    }
                }
            }

            if (!rows.Any(r => heads.TryGetValue(r.IdSalaryHead, out var h) && h.HeadType == SalaryHeadConstants.Earnings))
            {
                errors.Add("Add at least one earning.");
            }

            result.Errors = errors.Distinct().ToList();
            if (result.Errors.Any())
            {
                return result;
            }

            // ---------- Calculation ----------
            var valuesByCode = new Dictionary<string, decimal>();
            var valuesById = new Dictionary<int, decimal>();

            var ordered = rows
                .Select(r => new { Row = r, Head = heads[r.IdSalaryHead] })
                .OrderBy(x => x.Head.CalcSequence ?? int.MaxValue)
                .ThenBy(x => MethodOrder(x.Head.CalculationMethod))
                .ThenBy(x => x.Head.OrderNumber ?? int.MaxValue)
                .ToList();

            foreach (var x in ordered)
            {
                var head = x.Head;
                decimal value;
                decimal? fixedAmount = null;
                decimal? percentageValue = null;

                switch (head.CalculationMethod)
                {
                    case SalaryHeadConstants.FixedAmount:
                        fixedAmount = x.Row.FixedAmount ?? head.FixedValue ?? 0;
                        value = fixedAmount.Value;
                        break;

                    case SalaryHeadConstants.Percentage:
                        percentageValue = x.Row.PercentageValue ?? head.PercentageValue ?? 0;
                        var baseValue = head.IdPercentageSalaryHead.HasValue && valuesById.TryGetValue(head.IdPercentageSalaryHead.Value, out var b) ? b : 0;
                        if (head.WageCeiling.HasValue && baseValue > head.WageCeiling.Value)
                        {
                            baseValue = head.WageCeiling.Value;
                        }
                        value = baseValue * percentageValue.Value / 100;
                        break;

                    case SalaryHeadConstants.Formula:
                        var expression = SalaryHeadConstants.FormulaCode.Replace(head.CustomFormula ?? "0",
                            m => (valuesByCode.TryGetValue(m.Groups[1].Value, out var v) ? v : 0).ToString(CultureInfo.InvariantCulture));
                        value = FormulaEvaluator.Evaluate(expression);
                        break;

                    default:
                        value = 0;
                        break;
                }

                if (head.MinAmount.HasValue && value < head.MinAmount.Value) value = head.MinAmount.Value;
                if (head.MaxAmount.HasValue && value > head.MaxAmount.Value) value = head.MaxAmount.Value;
                value = ApplyRounding(value, head.RoundingRule);

                valuesById[head.IdSalaryHead] = value;
                valuesByCode[head.SalaryHeadCode] = value;

                var isMonthly = IsMonthlyHead(head);
                result.Rows.Add(new SalaryStructureRowDto
                {
                    IdSalaryHead = head.IdSalaryHead,
                    FixedAmount = fixedAmount,
                    PercentageValue = percentageValue,
                    CalculatedValue = value,
                    SalaryHeadCode = head.SalaryHeadCode,
                    SalaryHeadName = head.SalaryHeadName,
                    HeadType = head.HeadType,
                    CalculationMethod = head.CalculationMethod,
                    IdPercentageSalaryHead = head.IdPercentageSalaryHead,
                    PercentageOfSalaryHeadName = head.IdPercentageSalaryHead.HasValue && heads.TryGetValue(head.IdPercentageSalaryHead.Value, out var ph) ? ph.SalaryHeadName : null,
                    CustomFormula = head.CustomFormula,
                    CalcSequence = head.CalcSequence,
                    PayFrequency = head.PayFrequency,
                    DisbursingMonths = head.DisbursingMonths,
                    IsIncludedInMonthlyTotals = isMonthly,
                    PaidInNote = isMonthly ? null : BuildPaidInNote(head)
                });
            }

            // ---------- Totals ----------
            var monthly = result.Rows.Where(r => r.IsIncludedInMonthlyTotals).ToList();
            var earningsOnly = monthly.Where(r => r.HeadType == SalaryHeadConstants.Earnings).Sum(r => r.CalculatedValue);
            var reimbursements = monthly.Where(r => r.HeadType == SalaryHeadConstants.Reimbursement).Sum(r => r.CalculatedValue);

            result.TotalEarnings = earningsOnly + reimbursements;
            result.TotalDeductions = monthly.Where(r => r.HeadType == SalaryHeadConstants.Deduction).Sum(r => r.CalculatedValue);
            result.TotalEmployerContribution = monthly.Where(r => r.HeadType == SalaryHeadConstants.EmployerContribution).Sum(r => r.CalculatedValue);
            result.NetSalary = result.TotalEarnings - result.TotalDeductions;
            result.GrossMonthly = earningsOnly;
            result.CTCMonthly = result.TotalEarnings + result.TotalEmployerContribution;

            var annualExtras = result.Rows
                .Where(r => !r.IsIncludedInMonthlyTotals && r.HeadType != SalaryHeadConstants.Deduction)
                .Sum(r => r.CalculatedValue * PaymentsPerYear(heads[r.IdSalaryHead]));
            result.CTCAnnual = result.CTCMonthly * 12 + annualExtras;

            // Display order: Type, then Calculation Sequence
            result.Rows = result.Rows
                .OrderBy(r => HeadTypeDisplayOrder(r.HeadType))
                .ThenBy(r => r.CalcSequence ?? int.MaxValue)
                .ToList();

            return result;
        }

        /// <summary>Earnings, Reimbursement, Deduction, Employer Contribution.</summary>
        public static int HeadTypeDisplayOrder(string? headType) => headType switch
        {
            SalaryHeadConstants.Earnings => 0,
            SalaryHeadConstants.Reimbursement => 1,
            SalaryHeadConstants.Deduction => 2,
            SalaryHeadConstants.EmployerContribution => 3,
            _ => 4
        };

        private static int MethodOrder(string method) => method switch
        {
            SalaryHeadConstants.FixedAmount => 0,
            SalaryHeadConstants.Percentage => 1,
            SalaryHeadConstants.Formula => 2,
            _ => 3
        };

        private static decimal ApplyRounding(decimal value, string? rule) => (rule ?? "NEAREST").ToUpper() switch
        {
            "UP" => Math.Ceiling(value),
            "DOWN" => Math.Floor(value),
            "NONE" => Math.Round(value, 2, MidpointRounding.AwayFromZero),
            _ => Math.Round(value, 0, MidpointRounding.AwayFromZero)
        };

        /// <summary>A head paid only in certain months (DisbursingMonths set) or not monthly is left out of the monthly totals.</summary>
        public static bool IsMonthlyHead(SalaryHeads head) =>
            string.IsNullOrWhiteSpace(head.DisbursingMonths) &&
            (string.IsNullOrWhiteSpace(head.PayFrequency) || head.PayFrequency.ToUpper() == SalaryHeadConstants.Monthly);

        private static int PaymentsPerYear(SalaryHeads head)
        {
            if (!string.IsNullOrWhiteSpace(head.DisbursingMonths))
            {
                return head.DisbursingMonths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().Count();
            }
            return (head.PayFrequency ?? "").ToUpper() switch
            {
                "QUARTERLY" => 4,
                "HALF_YEARLY" => 2,
                "ANNUAL" => 1,
                "ONE_TIME" => 0,
                _ => 12
            };
        }

        public static string BuildPaidInNote(SalaryHeads head)
        {
            if (!string.IsNullOrWhiteSpace(head.DisbursingMonths))
            {
                var months = head.DisbursingMonths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(m => m.Length >= 3 ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(m.Substring(0, 3).ToLower()) : m);
                return "Paid in " + string.Join(", ", months);
            }
            return "Paid " + (head.PayFrequency ?? "").Replace("_", " ").ToLower();
        }
    }

    /// <summary>Evaluates + - * / and brackets on decimals. Input must already have [CODE]s replaced by numbers.</summary>
    public static class FormulaEvaluator
    {
        public static decimal Evaluate(string expression)
        {
            var tokens = Regex.Matches(expression, @"\d+(\.\d+)?|[+\-*/()]").Select(m => m.Value).ToList();
            var position = 0;
            var value = ParseExpression(tokens, ref position);
            if (position != tokens.Count)
            {
                throw new InvalidOperationException($"Invalid formula: {expression}");
            }
            return value;
        }

        private static decimal ParseExpression(List<string> t, ref int p)
        {
            var value = ParseTerm(t, ref p);
            while (p < t.Count && (t[p] == "+" || t[p] == "-"))
            {
                var op = t[p++];
                var right = ParseTerm(t, ref p);
                value = op == "+" ? value + right : value - right;
            }
            return value;
        }

        private static decimal ParseTerm(List<string> t, ref int p)
        {
            var value = ParseFactor(t, ref p);
            while (p < t.Count && (t[p] == "*" || t[p] == "/"))
            {
                var op = t[p++];
                var right = ParseFactor(t, ref p);
                value = op == "*" ? value * right : (right == 0 ? 0 : value / right);
            }
            return value;
        }

        private static decimal ParseFactor(List<string> t, ref int p)
        {
            if (p >= t.Count) throw new InvalidOperationException("Invalid formula: unexpected end.");
            var token = t[p++];
            if (token == "-") return -ParseFactor(t, ref p);
            if (token == "(")
            {
                var value = ParseExpression(t, ref p);
                if (p >= t.Count || t[p++] != ")") throw new InvalidOperationException("Invalid formula: missing ')'.");
                return value;
            }
            return decimal.Parse(token, CultureInfo.InvariantCulture);
        }
    }
}
