using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.PayrollManagement
{
    public class MaternityLeaveSalaryValidator : AbstractValidator<MaternityLeaveSalaryDto>
    {
        public MaternityLeaveSalaryValidator()
        {
            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("Employee ID must be greater than 0.");

            RuleFor(x => x.MaternityLeaveFrom)
                .LessThanOrEqualTo(x => x.MaternityLeaveTo)
                .WithMessage("Maternity leave 'From' date must be earlier than or equal to 'To' date.");
         
        }
    }
}

