using Microsoft.AspNetCore.Identity;

namespace CleanArchitecture.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Human-readable name shown in the UI. Optional: falls back to the user name when unset.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// A deactivated user is locked out until <see cref="DateTimeOffset.MaxValue"/> rather than
    /// deleted, so their audit trail survives. SignInManager already refuses to sign in a
    /// locked-out account, so no custom sign-in check is needed.
    /// </summary>
    public bool IsActive => LockoutEnd is null || LockoutEnd <= DateTimeOffset.UtcNow;
}
