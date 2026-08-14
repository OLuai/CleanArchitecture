namespace CleanArchitecture.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.UserName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(v => v.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(v => v.DisplayName)
            .MaximumLength(256);
    }
}
