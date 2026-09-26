using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.OfficeManagement
{
    public class EmployeeOfficePostingDtoValidator : AbstractValidator<EmployeeOfficePostingDto>
    {
        public EmployeeOfficePostingDtoValidator()
        {
            RuleFor(x => x.IdEmployee)
                .GreaterThan(0).WithMessage("Employee is required.");

            RuleFor(x => x.IdOffice)
                .GreaterThan(0).WithMessage("Office is required.");

            RuleFor(x => x.PostingFromDate)
                .NotEmpty().WithMessage("Posting from date is required.");

            RuleFor(x => x.PostingToDate)
                .GreaterThanOrEqualTo(x => x.PostingFromDate)
                .When(x => x.PostingToDate.HasValue)
                .WithMessage("Posting to date cannot be earlier than posting from date.");

            RuleFor(x => x.PostingType).MaximumLength(20).WithMessage("Posting type must not exceed 20 characters.");
            RuleFor(x => x.TransferOrderNumber).MaximumLength(50).WithMessage("Transfer order number must not exceed 50 characters.");
            RuleFor(x => x.PostingRemarks).MaximumLength(255).WithMessage("Posting remarks must not exceed 255 characters.");
        }
    }
}
