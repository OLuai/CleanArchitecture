using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Infrastructure.Identity;

/// <summary>
/// Resolves authorization policies of the form <c>Permission:&lt;value&gt;</c> on the fly so that
/// minimal-API endpoints can use <c>RequireAuthorization($"Permission:{Permissions.TodoLists.View}")</c>
/// without registering a policy per permission. Falls back to the default provider for any other name.
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[PolicyPrefix.Length..];

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(Permissions.ClaimType, permission)
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
