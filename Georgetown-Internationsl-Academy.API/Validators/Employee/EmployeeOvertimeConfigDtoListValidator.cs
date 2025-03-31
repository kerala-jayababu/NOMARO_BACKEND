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
               

                config.RuleFor(c => c.DayType)
    .NotEmpty().WithMessage("DayType is required.")
    .Must(type => new[] { "WORKINGDAY", "HOLIDAY", "PUBLICHOLIDAY" }
        .Contains(type?.Trim(), StringComparer.OrdinalIgnoreCase))
    .WithMessage("DayType must be one of the following: 'WORKINGDAY', 'HOLIDAY', 'PUBLICHOLIDAY'.");


                config.RuleFor(c => c.StandardRate)
                    .GreaterThan(0).WithMessage("StandardRate must be greater than 0.");

                config.RuleFor(c => c.DayRate)
                    .GreaterThanOrEqualTo(0).WithMessage("DayRate must be greater than or equal to 0.");
            });

            // Ensure DayType is unique
            RuleFor(x => x)
                .Must(configs =>
                {
                    var duplicates = configs
                        .GroupBy(c => c.DayType?.ToUpper().Trim())
                        .Where(g => g.Count() > 1)
                        .ToList();
                    return !duplicates.Any();
                })
                .WithMessage("Duplicate DayType values are not allowed.");
        }
    }

}
