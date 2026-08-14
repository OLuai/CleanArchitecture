using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Search;
using CleanArchitecture.Application.RoleManagement;
using CleanArchitecture.Application.Users;
using CleanArchitecture.Domain.Constants;
using DomainRoles = CleanArchitecture.Domain.Constants.Roles;

namespace CleanArchitecture.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    // Allow-list of sortable columns. A key absent from this map falls back to the default
    // ordering, so no caller-supplied string ever reaches the query.
    private static readonly SortMap<ApplicationUser> s_userSort = new SortMap<ApplicationUser>()
        .Add(nameof(UserDto.UserName), u => u.UserName)
        .Add(nameof(UserDto.Email), u => u.Email)
        .Add(nameof(UserDto.DisplayName), u => u.DisplayName)
        .Add(nameof(UserDto.LockoutEnd), u => u.LockoutEnd);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }

    // --- Administration surface ---

    public async Task<PaginatedList<UserDto>> GetUsersAsync(UserSearch search, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking();

        var pattern = SearchNormalizer.PreparePattern(search.Search);
        if (pattern is not null)
        {
            query = query.Where(u =>
                EF.Functions.Like(DbSearchExtensions.Unaccent(u.UserName!).ToLower(), pattern) ||
                EF.Functions.Like(DbSearchExtensions.Unaccent(u.Email!).ToLower(), pattern) ||
                (u.DisplayName != null && EF.Functions.Like(DbSearchExtensions.Unaccent(u.DisplayName).ToLower(), pattern)));
        }

        if (search.IsActive is { } isActive)
        {
            var now = DateTimeOffset.UtcNow;
            query = isActive
                ? query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now)
                : query.Where(u => u.LockoutEnd != null && u.LockoutEnd > now);
        }

        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            // Filtering by role means joining the Identity join tables, for which UserManager
            // exposes no queryable, so go through the role's user list.
            var usersInRole = await _userManager.GetUsersInRoleAsync(search.Role);
            var ids = usersInRole.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
            query = query.Where(u => ids.Contains(u.Id));
        }

        // ThenBy(Id) keeps paging deterministic when the sort key has duplicates.
        var ordered = s_userSort
            .Apply(query, search.SortBy, search.SortDescending, q => q.OrderBy(u => u.UserName))
            .ThenBy(u => u.Id);

        var page = await PaginatedList<ApplicationUser>.CreateAsync(
            ordered, search.PageNumber, search.PageSize, cancellationToken);

        var items = new List<UserDto>(page.Items.Count);
        foreach (var user in page.Items)
        {
            items.Add(await ToDtoAsync(user));
        }

        return new PaginatedList<UserDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    public async Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(NewUser newUser, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = newUser.UserName,
            Email = newUser.Email,
            DisplayName = newUser.DisplayName
        };

        var created = await _userManager.CreateAsync(user, newUser.Password);
        if (!created.Succeeded)
        {
            return (created.ToApplicationResult(), string.Empty);
        }

        if (newUser.Roles.Count > 0)
        {
            var assigned = await _userManager.AddToRolesAsync(user, newUser.Roles);
            if (!assigned.Succeeded)
            {
                // Roll back the half-created account rather than leaving it without its roles.
                await _userManager.DeleteAsync(user);
                return (assigned.ToApplicationResult(), string.Empty);
            }
        }

        return (Result.Success(), user.Id);
    }

    public async Task<Result> UpdateUserAsync(string userId, UserEdit edit, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserNotFound(userId);
        }

        user.DisplayName = edit.DisplayName;

        // SetUserNameAsync / SetEmailAsync run the uniqueness validators that a plain property
        // assignment would skip, and keep the normalised columns in sync.
        var renamed = await _userManager.SetUserNameAsync(user, edit.UserName);
        if (!renamed.Succeeded)
        {
            return renamed.ToApplicationResult();
        }

        var reEmailed = await _userManager.SetEmailAsync(user, edit.Email);
        if (!reEmailed.Succeeded)
        {
            return reEmailed.ToApplicationResult();
        }

        return (await _userManager.UpdateAsync(user)).ToApplicationResult();
    }

    public async Task<Result> SetUserRolesAsync(string userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserNotFound(userId);
        }

        var current = await _userManager.GetRolesAsync(user);
        var target = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = current.Where(r => !target.Contains(r)).ToArray();
        var toAdd = target.Where(r => !current.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();

        if (toRemove.Length > 0)
        {
            var removed = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded)
            {
                return removed.ToApplicationResult();
            }
        }

        if (toAdd.Length > 0)
        {
            var added = await _userManager.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded)
            {
                return added.ToApplicationResult();
            }
        }

        if (toRemove.Length > 0 || toAdd.Length > 0)
        {
            // Role claims are baked into the auth cookie at sign-in. Bumping the security stamp
            // makes that cookie stale, so SecurityStampValidator refreshes it (or signs the user
            // out) at the next validation interval instead of leaving them on old permissions.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> SetUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserNotFound(userId);
        }

        // An administrator resets without knowing the old password: remove, then set.
        var removed = await _userManager.RemovePasswordAsync(user);
        if (!removed.Succeeded)
        {
            return removed.ToApplicationResult();
        }

        return (await _userManager.AddPasswordAsync(user, newPassword)).ToApplicationResult();
    }

    public async Task<Result> SetUserActiveAsync(string userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserNotFound(userId);
        }

        var enabled = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!enabled.Succeeded)
        {
            return enabled.ToApplicationResult();
        }

        var lockedUntil = isActive ? (DateTimeOffset?)null : DateTimeOffset.MaxValue;
        var result = await _userManager.SetLockoutEndDateAsync(user, lockedUntil);
        if (!result.Succeeded)
        {
            return result.ToApplicationResult();
        }

        if (!isActive)
        {
            // Invalidate whatever session the user already has open.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Permissions = [.. claims
                    .Where(c => c.Type == Permissions.ClaimType)
                    .Select(c => c.Value)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(p => p, StringComparer.Ordinal)],
                UserCount = usersInRole.Count,
                IsBuiltIn = DomainRoles.All.Contains(role.Name!, StringComparer.Ordinal)
            });
        }

        return result;
    }

    public async Task<(Result Result, string RoleId)> CreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        var role = new IdentityRole(name);
        var result = await _roleManager.CreateAsync(role);

        return (result.ToApplicationResult(), result.Succeeded ? role.Id : string.Empty);
    }

    public async Task<Result> DeleteRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return Result.Success();
        }

        if (DomainRoles.All.Contains(role.Name!, StringComparer.Ordinal))
        {
            return Result.Failure([$"The built-in role '{role.Name}' cannot be deleted."]);
        }

        var members = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (members.Count > 0)
        {
            return Result.Failure([$"Role '{role.Name}' still has {members.Count} member(s). Reassign them first."]);
        }

        return (await _roleManager.DeleteAsync(role)).ToApplicationResult();
    }

    public async Task<Result> SetRolePermissionsAsync(string roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return Result.Failure([$"Role '{roleId}' was not found."]);
        }

        var unknown = permissions.Except(Permissions.All, StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            return Result.Failure([$"Unknown permission(s): {string.Join(", ", unknown)}."]);
        }

        var existing = await _roleManager.GetClaimsAsync(role);
        var current = existing.Where(c => c.Type == Permissions.ClaimType).ToArray();
        var target = permissions.ToHashSet(StringComparer.Ordinal);

        foreach (var claim in current.Where(c => !target.Contains(c.Value)))
        {
            var removed = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removed.Succeeded)
            {
                return removed.ToApplicationResult();
            }
        }

        var currentValues = current.Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var permission in target.Where(p => !currentValues.Contains(p)))
        {
            var added = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            if (!added.Succeeded)
            {
                return added.ToApplicationResult();
            }
        }

        // Everyone holding this role is carrying stale permission claims in their cookie.
        foreach (var member in await _userManager.GetUsersInRoleAsync(role.Name!))
        {
            await _userManager.UpdateSecurityStampAsync(member);
        }

        return Result.Success();
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName!,
        Email = user.Email,
        DisplayName = user.DisplayName,
        EmailConfirmed = user.EmailConfirmed,
        IsActive = user.IsActive,
        LockoutEnd = user.LockoutEnd,
        Roles = [.. (await _userManager.GetRolesAsync(user)).OrderBy(r => r, StringComparer.Ordinal)]
    };

    private static Result UserNotFound(string userId) => Result.Failure([$"User '{userId}' was not found."]);
}
