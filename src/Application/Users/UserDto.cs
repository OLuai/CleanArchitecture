namespace CleanArchitecture.Application.Users;

/// <summary>
/// A user as exposed by the administration surface. Permissions are not carried here: they are
/// derived from <see cref="Roles"/>, and the roles screen is where they are edited.
/// </summary>
public record UserDto
{
    public required string Id { get; init; }

    public required string UserName { get; init; }

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public bool EmailConfirmed { get; init; }

    /// <summary>False when the account is locked out — see <c>SetUserActiveCommand</c>.</summary>
    public bool IsActive { get; init; }

    public DateTimeOffset? LockoutEnd { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }
}
