using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using DomainPermissions = CleanArchitecture.Domain.Constants.Permissions;

namespace CleanArchitecture.Application.RoleManagement.Commands.SetRolePermissions;

/// <summary>Replaces the permissions a role grants with the supplied set.</summary>
[Authorize(Permissions = DomainPermissions.Roles.Manage)]
public record SetRolePermissionsCommand : IRequest
{
    public string RoleId { get; init; } = string.Empty;

    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand>
{
    private readonly IIdentityService _identityService;

    public SetRolePermissionsCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.SetRolePermissionsAsync(request.RoleId, request.Permissions, cancellationToken);

        result.EnsureSuccess();
    }
}
