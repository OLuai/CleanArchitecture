namespace CleanArchitecture.Application.Settings.Commands.SaveEmailSettings;

public class SaveEmailSettingsCommandValidator : AbstractValidator<SaveEmailSettingsCommand>
{
    public SaveEmailSettingsCommandValidator()
    {
        RuleFor(v => v.Host)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(v => v.Port)
            .InclusiveBetween(1, 65535);

        RuleFor(v => v.UserName)
            .MaximumLength(255);

        RuleFor(v => v.Password)
            .MaximumLength(255);

        RuleFor(v => v.FromAddress)
            .NotEmpty()
            .MaximumLength(255)
            .EmailAddress();

        RuleFor(v => v.FromName)
            .MaximumLength(100);
    }
}
