using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.Employee
{
    public class EmployeeActionPostDtoListValidator : AbstractValidator<List<EmployeeActionPostDto>>
    {
        public EmployeeActionPostDtoListValidator()
        {
            RuleFor(list => list)
                .NotNull().WithMessage("Employee action list cannot be null.")
                .Must(list => list.Any()).WithMessage("Employee action list cannot be empty.");

            RuleForEach(list => list).ChildRules(action =>
            {
                action.RuleFor(x => x.IdEmployee)
                    .NotEmpty()
                    .WithMessage("IdEmployee is required.");

                action.RuleFor(x => x.ActionType)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("ActionType is required.")
                    .Must(type =>
                    {
                        var t = type?.Trim().ToUpperInvariant();
                        return t == "RECOGNITION" || t == "DISCIPLINARY";
                    })
                    .WithMessage("ActionType must be either 'RECOGNITION' or 'DISCIPLINARY'.");

                action.RuleFor(x => x.ActionSeverity)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("ActionSeverity is required.")
                    .Must(sev =>
                    {
                        var s = sev?.Trim().ToUpperInvariant();
                        return s == "LOW" || s == "MEDIUM" || s == "HIGH" || s == "VERY HIGH";
                    })
                    .WithMessage("ActionSeverity must be one of: Low, Medium, High, Very High.");

                action.RuleFor(x => x.Status)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Status is required.")
                    .Must(status =>
                    {
                        var st = status?.Trim().ToUpperInvariant();
                        return st == "ACTIVE" || st == "DISABLED";
                    })
                    .WithMessage("Status must be either 'Active' or 'Disabled'.");

                action.RuleFor(x => x.ActionDescription)
                    .NotEmpty()
                    .WithMessage("ActionDescription is required.")
                    .MaximumLength(500)
                    .WithMessage("ActionDescription must not exceed 500 characters.");

                action.RuleFor(x => x.Remarks)
                    .MaximumLength(500)
                    .WithMessage("Remarks must not exceed 500 characters.");

                action.RuleFor(x => x.EffectiveFromDate)
                    .NotEmpty()
                    .WithMessage("EffectiveFromDate is required.");

                action.RuleFor(x => x.EffectiveToDate)
                    .Must((dto, toDate) =>
                    {
                        if (!toDate.HasValue) return true;
                        return toDate.Value.Date >= dto.EffectiveFromDate.Date;
                    })
                    .WithMessage("EffectiveToDate cannot be less than EffectiveFromDate.");
            });

            // ✅ Optional: prevent duplicate same IdEmployeeAction inside list
            RuleFor(list => list)
                .Must(list =>
                {
                    var ids = list.Where(x => x.IdEmployeeAction > 0).Select(x => x.IdEmployeeAction).ToList();
                    return ids.Distinct().Count() == ids.Count;
                })
                .WithMessage("Duplicate IdEmployeeAction found in the request list.");
        }
    } }

