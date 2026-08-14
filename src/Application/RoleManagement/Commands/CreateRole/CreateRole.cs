using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using DomainPermissions = CleanArchitecture.Domain.Constants.Permissions;

namespace CleanArchitecture.Application.RoleManagement.Commands.CreateRole;

[Authorize(Permissions = DomainPermissions.Roles.Manage)]
public record CreateRoleCommand : IRequest<string>
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, string>
{
    private readonly IIdentityService _identityService;

    public CreateRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<string> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var (result, roleId) = await _identityService.CreateRoleAsync(request.Name, cancellationToken);
        result.EnsureSuccess();

        if (request.Permissions.Count > 0)
        {
            var granted = await _identityService.SetRolePermissionsAsync(roleId, request.Permissions, cancellationToken);
            granted.EnsureSuccess();
        }

        return roleId;
    }
}
