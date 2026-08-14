using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.RoleManagement.Queries.GetPermissions;

/// <summary>
/// The permission catalogue, grouped by the area each permission belongs to, so the role editor
/// can render a checkbox matrix without hard-coding the list on the client.
/// </summary>
[Authorize(Permissions = Permissions.Roles.View)]
public record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;

public record PermissionGroupDto(string Area, IReadOnlyList<string> Permissions);

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        // Permission values are "Area.Action", so the area is everything before the first dot.
        IReadOnlyList<PermissionGroupDto> groups =
        [
            .. Permissions.All
                .GroupBy(p => p.Split('.', 2)[0], StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new PermissionGroupDto(g.Key, [.. g.OrderBy(p => p, StringComparer.Ordinal)]))
        ];

        return Task.FromResult(groups);
    }
}
