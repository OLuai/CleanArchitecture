using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.RoleManagement;
using CleanArchitecture.Application.Users;

namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// The Application layer's window onto ASP.NET Identity. Identity types (ApplicationUser,
/// IdentityRole, UserManager) live in Infrastructure, so everything crosses this boundary as
/// Application DTOs — including paging, which the implementation performs over
/// <c>UserManager.Users</c>.
/// </summary>
public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    // --- Administration surface ---

    Task<PaginatedList<UserDto>> GetUsersAsync(UserSearch search, CancellationToken cancellationToken);

    Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken);

    Task<(Result Result, string UserId)> CreateUserAsync(NewUser user, CancellationToken cancellationToken);

    Task<Result> UpdateUserAsync(string userId, UserEdit edit, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the user's roles with <paramref name="roles"/> and refreshes their security stamp,
    /// so any open session picks up the new claims at the next validation interval.
    /// </summary>
    Task<Result> SetUserRolesAsync(string userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);

    Task<Result> SetUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken);

    /// <summary>Deactivating locks the account out indefinitely rather than deleting it.</summary>
    Task<Result> SetUserActiveAsync(string userId, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);

    Task<(Result Result, string RoleId)> CreateRoleAsync(string name, CancellationToken cancellationToken);

    Task<Result> DeleteRoleAsync(string roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the role's permission claims and refreshes the security stamp of every user in
    /// that role, so their open sessions pick up the change.
    /// </summary>
    Task<Result> SetRolePermissionsAsync(string roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);
}

/// <summary>Filter, sort and paging criteria for the user list.</summary>
public record UserSearch
{
    public string? Search { get; init; }

    public string? Role { get; init; }

    public bool? IsActive { get; init; }

    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public string? SortBy { get; init; }

    public bool? SortDescending { get; init; }
}

public record NewUser(string UserName, string Email, string Password, string? DisplayName, IReadOnlyCollection<string> Roles);

public record UserEdit(string UserName, string Email, string? DisplayName);
