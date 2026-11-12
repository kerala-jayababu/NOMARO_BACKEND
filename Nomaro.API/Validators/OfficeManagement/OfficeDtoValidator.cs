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
            RuleFor(x => x.TradeLicenceNumber).MaximumLength(50).WithMessage("Trade Licence No. must not exceed 50 characters.");
            RuleFor(x => x.EstablishmentId).MaximumLength(20).WithMessage("Establishment ID must not exceed 20 characters.")
                .Matches("^[0-9]+$").When(x => !string.IsNullOrWhiteSpace(x.EstablishmentId)).WithMessage("Establishment ID must contain digits only.");
            RuleFor(x => x.PFEstablishmentCode).MaximumLength(30).WithMessage("PF Establishment Code must not exceed 30 characters.");
            RuleFor(x => x.ESIEmployerCode).Matches("^[0-9]{17}$").When(x => !string.IsNullOrWhiteSpace(x.ESIEmployerCode)).WithMessage("ESI Employer Code must be 17 digits.");
            RuleFor(x => x.TAN).Matches("^[A-Za-z]{4}[0-9]{5}[A-Za-z]$").When(x => !string.IsNullOrWhiteSpace(x.TAN)).WithMessage("TAN must be 10 characters, like ABCD12345E.");
            RuleFor(x => x.PTRegistrationNumber).MaximumLength(50).WithMessage("PT Registration No. must not exceed 50 characters.");

            // "latitude,longitude" with latitude -90..90 and longitude -180..180
            RuleFor(x => x.GeoCoordinate)
                .MaximumLength(40).WithMessage("Geo coordinate must not exceed 40 characters.")
                .Must(BeValidGeoCoordinate).WithMessage("Geo coordinate must be latitude,longitude (e.g. 9.931233,76.267304).")
                .When(x => !string.IsNullOrWhiteSpace(x.GeoCoordinate));

            RuleFor(x => x.EmailId)
                .MaximumLength(150).WithMessage("Email ID must not exceed 150 characters.")
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.EmailId))
                .WithMessage("Invalid email ID.");

            RuleFor(x => x.ClosedDate)
                .GreaterThanOrEqualTo(x => x.OpenedDate)
                .When(x => x.OpenedDate.HasValue && x.ClosedDate.HasValue)
                .WithMessage("Closed date cannot be earlier than opened date.");
        }
    

        private static bool BeValidGeoCoordinate(string? value)
        {
            var parts = (value ?? string.Empty).Split(',');
            return parts.Length == 2
                && decimal.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                && decimal.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng)
                && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
        }
    }
}
