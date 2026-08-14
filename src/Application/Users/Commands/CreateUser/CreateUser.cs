using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Commands.CreateUser;

[Authorize(Permissions = Permissions.Users.Create)]
public record CreateUserCommand : IRequest<string>
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IIdentityService _identityService;

    public CreateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(
            new NewUser(request.UserName, request.Email, request.Password, request.DisplayName, request.Roles),
            cancellationToken);

        result.EnsureSuccess();

        return userId;
    }
}
