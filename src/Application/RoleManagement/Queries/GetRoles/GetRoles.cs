using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.RoleManagement.Queries.GetRoles;

/// <summary>
/// Every role with the permissions it grants and its member count. Small and rarely changing,
/// so it is returned unpaginated.
/// </summary>
[Authorize(Permissions = Permissions.Roles.View)]
public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IIdentityService _identityService;

    public GetRolesQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        => _identityService.GetRolesAsync(cancellationToken);
}
