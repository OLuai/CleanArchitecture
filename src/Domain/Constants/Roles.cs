namespace CleanArchitecture.Domain.Constants;

/// <summary>
/// Roles seeded by <c>ProductionSeeder</c>. Permissions are granted to roles, never directly to
/// users, so a user's effective permissions are the union of their roles' claims. See
/// <c>RolePermissionMap</c> for the role-to-permission assignment.
/// </summary>
public abstract class Roles
{
    /// <summary>Full access. Automatically granted every permission in the catalogue.</summary>
    public const string Administrator = nameof(Administrator);

    /// <summary>Default role for a regular signed-in user.</summary>
    public const string User = nameof(User);

    public static IReadOnlyList<string> All { get; } = [Administrator, User];
}
