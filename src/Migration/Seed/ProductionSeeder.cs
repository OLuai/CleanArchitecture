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
        var adminRole = await _roleManager.FindByNameAsync(Roles.Administrator);
        if (adminRole is null)
        {
            _logger.LogInformation("Creating role {Role}", Roles.Administrator);
            adminRole = new IdentityRole(Roles.Administrator);
            var create = await _roleManager.CreateAsync(adminRole);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role {Roles.Administrator}: {string.Join(", ", create.Errors.Select(e => e.Description))}");
            }
        }

        await SyncAdministratorPermissionsAsync(adminRole);
    }

    private async Task SyncAdministratorPermissionsAsync(IdentityRole adminRole)
    {
        var existing = await _roleManager.GetClaimsAsync(adminRole);
        var alreadyGranted = existing
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in Permissions.All)
        {
            if (alreadyGranted.Contains(permission))
            {
                continue;
            }

            _logger.LogInformation("Granting permission {Permission} to role {Role}", permission, Roles.Administrator);
            var result = await _roleManager.AddClaimAsync(adminRole, new Claim(Permissions.ClaimType, permission));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to grant permission {permission} to {Roles.Administrator}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
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
            Email = DefaultAdminUserName
        };

        var create = await _userManager.CreateAsync(administrator, DefaultAdminPassword);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create default administrator: {string.Join(", ", create.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(administrator, Roles.Administrator);
    }
}
