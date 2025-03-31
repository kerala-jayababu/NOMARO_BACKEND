using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Validators.LeavePassage
{
    public class LeavePassageValidator : AbstractValidator<LeavePassageDto>
    {
        public LeavePassageValidator()
        {
            RuleFor(x => x.IdEmployee).GreaterThan(0);
            RuleFor(x => x.IdFinancialYear).GreaterThan(0);
            RuleFor(x => x.IdSalaryMonth).GreaterThan(0);
            
        }
    }

}
