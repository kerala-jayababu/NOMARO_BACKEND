using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.LeavePassage
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

