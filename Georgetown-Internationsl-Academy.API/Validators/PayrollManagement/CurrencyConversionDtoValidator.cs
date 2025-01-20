using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using System.Collections.Generic;

public class CurrencyConversionDtoValidator : AbstractValidator<CurrencyConversionDto>
{
    public CurrencyConversionDtoValidator()
    {
        // Define allowed currencies
        List<string> allowedCurrencies = new List<string> { "GYD", "USD", "IND" };

        // FromCurrency validation
        RuleFor(x => x.FromCurrency)
            .NotEmpty().WithMessage("FromCurrency is required.")
            .Must(value => allowedCurrencies.Contains(value))
            .WithMessage($"FromCurrency must be one of the following: {string.Join(", ", allowedCurrencies)}.");

        // ToCurrency validation
        RuleFor(x => x.ToCurrency)
            .NotEmpty().WithMessage("ToCurrency is required.")
            .Must(value => allowedCurrencies.Contains(value))
            .WithMessage($"ToCurrency must be one of the following: {string.Join(", ", allowedCurrencies)}.");

        // Ensure FromCurrency and ToCurrency are not the same
        RuleFor(x => x)
            .Must(x => x.FromCurrency != x.ToCurrency)
            .WithMessage("FromCurrency and ToCurrency cannot be the same.");

        // RateDate validation
        RuleFor(x => x.RateDate)
            .NotEmpty().WithMessage("RateDate is required.");

        // ConversionRate validation
        RuleFor(x => x.ConversionRate)
            .GreaterThan(0).WithMessage("ConversionRate must be greater than 0.");
    }
}
