using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using DomainRoles = CleanArchitecture.Domain.Constants.Roles;

namespace CleanArchitecture.Application.Users.Commands.SetUserRoles;

/// <summary>Replaces a user's roles with the supplied set.</summary>
[Authorize(Permissions = Permissions.Users.ManageRoles)]
public record SetUserRolesCommand : IRequest
{
    public string UserId { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];
}

public class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public SetUserRolesCommandHandler(IIdentityService identityService, IUser user)
    {
        _identityService = identityService;
        _user = user;
    }

    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        // Dropping your own Administrator role revokes the permission you need to put it back.
        if (string.Equals(request.UserId, _user.Id, StringComparison.Ordinal)
            && !request.Roles.Contains(DomainRoles.Administrator, StringComparer.Ordinal))
        {
            throw new FriendlyException("Vous ne pouvez pas retirer votre propre rôle Administrateur.");
        }

        var result = await _identityService.SetUserRolesAsync(request.UserId, request.Roles, cancellationToken);

        result.EnsureSuccess();
    }
}
