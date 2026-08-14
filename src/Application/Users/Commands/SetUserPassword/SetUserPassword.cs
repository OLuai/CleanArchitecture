using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Commands.SetUserPassword;

/// <summary>
/// Administrative password reset: sets a new password without knowing the current one. A user
/// changing their own password goes through the Identity <c>manage/info</c> endpoint instead,
/// which requires the old password.
/// </summary>
[Authorize(Permissions = Permissions.Users.Update)]
public record SetUserPasswordCommand : IRequest
{
    public string UserId { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}

public class SetUserPasswordCommandHandler : IRequestHandler<SetUserPasswordCommand>
{
    private readonly IIdentityService _identityService;

    public SetUserPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(SetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.SetUserPasswordAsync(request.UserId, request.NewPassword, cancellationToken);

        result.EnsureSuccess();
    }
}
