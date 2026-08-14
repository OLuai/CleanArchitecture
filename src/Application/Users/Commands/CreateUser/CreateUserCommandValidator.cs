namespace CleanArchitecture.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(v => v.UserName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(v => v.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        // Length is the only rule enforced here; complexity is left to IdentityOptions.Password,
        // which is the single place those rules are configured.
        RuleFor(v => v.Password)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(v => v.DisplayName)
            .MaximumLength(256);

        RuleForEach(v => v.Roles)
            .NotEmpty();
    }
}
