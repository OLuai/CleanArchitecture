namespace CleanArchitecture.Application.RoleManagement.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(256)
            .Matches("^[A-Za-z0-9 _-]+$")
                .WithMessage("'{PropertyName}' may only contain letters, digits, spaces, hyphens and underscores.");
    }
}
