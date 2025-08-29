using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.Employee
{
    public class EmployeeOvertimeConfigDtoListValidator : AbstractValidator<List<EmployeeOvertimeConfigDtoList>>
    {
        public EmployeeOvertimeConfigDtoListValidator()
        {
            RuleForEach(x => x).ChildRules(config =>
            {
                // DayType validation (only if provided)
                config.RuleFor(c => c.DayType)
                    .Must(type => string.IsNullOrWhiteSpace(type) ||
                        new[] { "WORKINGDAY", "HOLIDAY", "PUBLICHOLIDAY" }
                            .Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage("DayType must be one of the following: 'WORKINGDAY', 'HOLIDAY', 'PUBLICHOLIDAY'.");

                // StandardRate validation (only if provided)
                config.RuleFor(c => c.StandardRate)
                    .GreaterThan(0)
                    .When(c => c.StandardRate.HasValue)
                    .WithMessage("StandardRate must be greater than 0.");

                // DayRate validation (only if provided)
                config.RuleFor(c => c.DayRate)
                    .GreaterThanOrEqualTo(0)
                    .When(c => c.DayRate.HasValue)
                    .WithMessage("DayRate must be greater than or equal to 0.");
            });

            // Ensure DayType is unique, but ignore null/empty
            RuleFor(x => x)
                .Must(configs =>
                {
                    var duplicates = configs
                        .Where(c => !string.IsNullOrWhiteSpace(c.DayType))
                        .GroupBy(c => c.DayType.Trim().ToUpper())
                        .Where(g => g.Count() > 1)
                        .ToList();
                    return !duplicates.Any();
                })
                .WithMessage("Duplicate DayType values are not allowed.");
        }
    }
}
