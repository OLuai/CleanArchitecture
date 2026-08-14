using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Migration.Seed;

/// <summary>
/// Which permissions each seeded role grants. This is the desired state: the seeder both grants
/// what is missing and revokes what is no longer listed, so removing a line here actually takes
/// the permission away on the next deployment.
/// <para>
/// Administrator is deliberately absent — it is granted the whole catalogue, so a new permission
/// never needs to be added in two places.
/// </para>
/// </summary>
public static class RolePermissionMap
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Roles.User] =
            [
                Permissions.TodoLists.View,
                Permissions.TodoLists.Create,
                Permissions.TodoLists.Update,
                Permissions.TodoLists.Delete,
                Permissions.TodoItems.View,
                Permissions.TodoItems.Create,
                Permissions.TodoItems.Update,
                Permissions.TodoItems.Delete,
                Permissions.WeatherForecasts.View
            ]
        };

    /// <summary>The permissions a role should hold. Administrator gets everything.</summary>
    public static IReadOnlyList<string> For(string role) =>
        string.Equals(role, Roles.Administrator, StringComparison.Ordinal)
            ? Permissions.All
            : ByRole.TryGetValue(role, out var permissions) ? permissions : [];
}
