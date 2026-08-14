using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Infrastructure.Identity;

namespace CleanArchitecture.Migration.Seed;

public class ProductionSeeder : IDbSeeder
{
    private const string DefaultAdminUserName = "administrator@localhost";
    private const string DefaultAdminPassword = "Administrator1!";

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProductionSeeder> _logger;

    public ProductionSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ProductionSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public virtual async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync();
        await SeedAdministratorAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in Roles.All)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                _logger.LogInformation("Creating role {Role}", roleName);
                role = new IdentityRole(roleName);
                var create = await _roleManager.CreateAsync(role);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role {roleName}: {Describe(create)}");
                }
            }

            await SyncPermissionsAsync(role, RolePermissionMap.For(roleName));
        }
    }

    /// <summary>
    /// Brings the role's permission claims in line with <paramref name="desired"/>, in both
    /// directions. Granting alone would leave a permission in place forever once it was removed
    /// from the map or renamed in the catalogue.
    /// </summary>
    private async Task SyncPermissionsAsync(IdentityRole role, IReadOnlyList<string> desired)
    {
        var existing = await _roleManager.GetClaimsAsync(role);
        var current = existing.Where(c => c.Type == Permissions.ClaimType).ToArray();
        var target = desired.ToHashSet(StringComparer.Ordinal);

        foreach (var stale in current.Where(c => !target.Contains(c.Value)))
        {
            _logger.LogInformation("Revoking permission {Permission} from role {Role}", stale.Value, role.Name);
            var removed = await _roleManager.RemoveClaimAsync(role, stale);
            if (!removed.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to revoke permission {stale.Value} from {role.Name}: {Describe(removed)}");
            }
        }

        var granted = current.Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var permission in target.Where(p => !granted.Contains(p)))
        {
            _logger.LogInformation("Granting permission {Permission} to role {Role}", permission, role.Name);
            var added = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to grant permission {permission} to {role.Name}: {Describe(added)}");
            }
        }
    }

    private async Task SeedAdministratorAsync()
    {
        var existing = await _userManager.FindByNameAsync(DefaultAdminUserName);
        if (existing is not null)
        {
            return;
        }

        _logger.LogInformation("Creating default administrator {UserName}", DefaultAdminUserName);
        var administrator = new ApplicationUser
        {
            UserName = DefaultAdminUserName,
            Email = DefaultAdminUserName,
            DisplayName = "Administrator"
        };

        var create = await _userManager.CreateAsync(administrator, DefaultAdminPassword);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create default administrator: {Describe(create)}");
        }

        await _userManager.AddToRoleAsync(administrator, Roles.Administrator);
    }

    private static string Describe(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));
}
