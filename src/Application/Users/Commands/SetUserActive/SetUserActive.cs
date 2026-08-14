using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Commands.SetUserActive;

/// <summary>
/// Activates or deactivates an account. Deactivating locks it out indefinitely instead of
/// deleting it, so history and audit references survive.
/// </summary>
[Authorize(Permissions = Permissions.Users.Update)]
public record SetUserActiveCommand(string UserId, bool IsActive) : IRequest;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public SetUserActiveCommandHandler(IIdentityService identityService, IUser user)
    {
        _identityService = identityService;
        _user = user;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsActive && string.Equals(request.UserId, _user.Id, StringComparison.Ordinal))
        {
            throw new FriendlyException("Vous ne pouvez pas désactiver votre propre compte.");
        }

        var result = await _identityService.SetUserActiveAsync(request.UserId, request.IsActive, cancellationToken);

        result.EnsureSuccess();
    }
}
