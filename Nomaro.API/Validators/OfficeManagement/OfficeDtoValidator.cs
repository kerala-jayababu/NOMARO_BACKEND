using FluentValidation;
using Nomaro.API.DTO;

namespace Nomaro.API.Validators.OfficeManagement
{
    public class OfficeDtoValidator : AbstractValidator<OfficeDto>
    {
        public OfficeDtoValidator()
        {
            RuleFor(x => x.OfficeCode)
                .NotEmpty().WithMessage("Office code is required.")
                .MaximumLength(20).WithMessage("Office code must not exceed 20 characters.");

            RuleFor(x => x.OfficeName)
                .NotEmpty().WithMessage("Office name is required.")
                .MaximumLength(150).WithMessage("Office name must not exceed 150 characters.");

            RuleFor(x => x.IdOfficeType)
                .GreaterThan(0).WithMessage("Office type is required.");

            RuleFor(x => x.IdParentOffice)
                .GreaterThan(0).When(x => x.IdParentOffice.HasValue)
                .WithMessage("Invalid parent office.");

            RuleFor(x => x.IdParentOffice)
                .NotEqual(x => x.IdOffice).When(x => x.IdOffice > 0 && x.IdParentOffice.HasValue)
                .WithMessage("An office cannot be its own parent office.");

            RuleFor(x => x.AddressLine1).MaximumLength(200).WithMessage("Address line 1 must not exceed 200 characters.");
            RuleFor(x => x.AddressLine2).MaximumLength(200).WithMessage("Address line 2 must not exceed 200 characters.");
            RuleFor(x => x.City).MaximumLength(100).WithMessage("City must not exceed 100 characters.");
            RuleFor(x => x.District).MaximumLength(100).WithMessage("District must not exceed 100 characters.");
            RuleFor(x => x.State).MaximumLength(100).WithMessage("State must not exceed 100 characters.");
            RuleFor(x => x.Country).MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
            RuleFor(x => x.PinCode).MaximumLength(10).WithMessage("Pin code must not exceed 10 characters.");
            RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
            RuleFor(x => x.GSTIN).MaximumLength(15).WithMessage("GSTIN must not exceed 15 characters.");

            RuleFor(x => x.EmailId)
                .MaximumLength(150).WithMessage("Email ID must not exceed 150 characters.")
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.EmailId))
                .WithMessage("Invalid email ID.");

            RuleFor(x => x.ClosedDate)
                .GreaterThanOrEqualTo(x => x.OpenedDate)
                .When(x => x.OpenedDate.HasValue && x.ClosedDate.HasValue)
                .WithMessage("Closed date cannot be earlier than opened date.");
        }
    }
}
