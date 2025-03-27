using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
{
    public class OvertimeTransactionDtoValidator : AbstractValidator<OvertimeTransactionDto>
    {
        public OvertimeTransactionDtoValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0).WithMessage("Employee ID must be greater than 0.");           
            RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start Date must be less than or equal to End Date.");
            RuleFor(x => x.DurationInHours).GreaterThan(0).WithMessage("Duration in hours must be greater than 0.");
            RuleFor(x => x.ReasonForOvertime).NotEmpty().WithMessage("Reason for overtime is required.");
        }
    }
}
