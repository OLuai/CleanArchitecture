namespace CleanArchitecture.Application.RoleManagement;

/// <summary>A role and the permissions it grants (stored as role claims).</summary>
public record RoleDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<string> Permissions { get; init; }

    /// <summary>Number of users currently assigned this role.</summary>
    public int UserCount { get; init; }

    /// <summary>
    /// True for roles the application depends on (see <c>Domain.Constants.Roles.All</c>).
    /// They can have their permissions edited but must not be renamed or deleted.
    /// </summary>
    public bool IsBuiltIn { get; init; }
}
