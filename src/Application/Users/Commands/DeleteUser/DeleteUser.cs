using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Commands.DeleteUser;

[Authorize(Permissions = Permissions.Users.Delete)]
public record DeleteUserCommand(string UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public DeleteUserCommandHandler(IIdentityService identityService, IUser user)
    {
        _identityService = identityService;
        _user = user;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Deleting your own account from the admin screen would lock you out mid-session, and is
        // never what the click meant.
        if (string.Equals(request.UserId, _user.Id, StringComparison.Ordinal))
        {
            throw new FriendlyException("Vous ne pouvez pas supprimer votre propre compte.");
        }

        var result = await _identityService.DeleteUserAsync(request.UserId);

        result.EnsureSuccess();
    }
}
