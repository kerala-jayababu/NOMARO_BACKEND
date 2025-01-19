using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

public class VacationModeDtoValidator : AbstractValidator<VacationModeDto>
{
    public VacationModeDtoValidator()
    {
        RuleFor(x => x.IdEmployee)
            .GreaterThan(0).WithMessage("IdEmployee must be greater than 0.");

        RuleFor(x => x.VacationFrom)
            .LessThanOrEqualTo(x => x.VacationTo).WithMessage("VacationFrom must be earlier than or equal to VacationTo.");

        RuleFor(x => x.VacationTo)
            .GreaterThanOrEqualTo(x => x.VacationFrom).WithMessage("VacationTo must be later than or equal to VacationFrom.");

        RuleFor(x => x.IdSubstitueEmployee)
            .GreaterThan(0).WithMessage("IdSubstituteEmployee must be greater than 0.");
    }
}
