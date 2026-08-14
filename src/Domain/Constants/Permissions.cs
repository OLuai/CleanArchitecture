using System.Reflection;

namespace CleanArchitecture.Domain.Constants;

/// <summary>
/// Catalogue des permissions de l'application. Source unique de vérité utilisée par le
/// backend (AuthorizationBehaviour, AuthorizationPolicyProvider, seeding des RoleClaims)
/// et par le frontend (exposé via l'OpenAPI sous forme d'enum TypeScript).
/// </summary>
public static class Permissions
{
    public static class TodoLists
    {
        public const string View = "TodoLists.View";
        public const string Create = "TodoLists.Create";
        public const string Update = "TodoLists.Update";
        public const string Delete = "TodoLists.Delete";
    }

    public static class TodoItems
    {
        public const string View = "TodoItems.View";
        public const string Create = "TodoItems.Create";
        public const string Update = "TodoItems.Update";
        public const string Delete = "TodoItems.Delete";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";

        /// <summary>Assign or revoke a user's roles.</summary>
        public const string ManageRoles = "Users.ManageRoles";
    }

    public static class Roles
    {
        public const string View = "Roles.View";

        /// <summary>Create or delete roles and edit the permissions they grant.</summary>
        public const string Manage = "Roles.Manage";
    }

    public static class WeatherForecasts
    {
        public const string View = "WeatherForecasts.View";
    }

    public const string ClaimType = "permission";

    public static IReadOnlyList<string> All { get; } = typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToArray();
}
