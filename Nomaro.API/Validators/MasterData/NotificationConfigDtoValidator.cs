using FluentValidation;
using Nomaro.API.DTO;

public class NotificationConfigDtoValidator : AbstractValidator<NotificationConfigDto>
{
    public NotificationConfigDtoValidator()
    {
        // NotificationType (Required)
        RuleFor(x => x.NotificationType)
            .NotEmpty().WithMessage("NotificationType is required.")
            .MaximumLength(100).WithMessage("NotificationType must not exceed 100 characters.");

        // EmailSubject (Optional, can be null)
        RuleFor(x => x.EmailSubject)
            .MaximumLength(100).WithMessage("EmailSubject must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.EmailSubject)); // Only validate if not null or empty

        // EmailContent (Optional)
        RuleFor(x => x.EmailContent)
            .MaximumLength(2000).WithMessage("EmailContent must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.EmailContent));

        // AppNotificationText (Optional)
        RuleFor(x => x.AppNotificationText)
            .MaximumLength(500).WithMessage("AppNotificationText must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AppNotificationText));

        // WebLink (Optional)
        RuleFor(x => x.WebLink)
            .MaximumLength(500).WithMessage("WebLink must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.WebLink));
    }
}

