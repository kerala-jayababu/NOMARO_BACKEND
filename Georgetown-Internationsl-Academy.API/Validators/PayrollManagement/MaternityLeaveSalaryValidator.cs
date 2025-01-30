using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.PayrollManagement
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

            RuleFor(x => x.NetSalary)
                .GreaterThan(0).WithMessage("Net Salary must be greater than 0.");

            RuleFor(x => x.MaternityLeaveSalary)
                .GreaterThan(0).WithMessage("Maternity Leave Salary must be greater than 0.");
        }
    }
}
