using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Users;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using CleanArchitecture.Application.Users.Commands.DeleteUser;
using CleanArchitecture.Application.Users.Commands.SetUserActive;
using CleanArchitecture.Application.Users.Commands.SetUserPassword;
using CleanArchitecture.Application.Users.Commands.SetUserRoles;
using CleanArchitecture.Application.Users.Commands.UpdateUser;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Application.Users.Queries.GetUsers;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Web.Infrastructure;
using DomainPermissions = CleanArchitecture.Domain.Constants.Permissions;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        // Keep every built-in Identity flow available (refresh, confirmEmail, resendConfirmationEmail,
        // forgotPassword, resetPassword, manage/2fa, manage/info) under /api/Users/identity/*.
        // The canonical login/register paths are reserved for the custom endpoints below.
        groupBuilder.MapGroup("identity").MapIdentityApi<ApplicationUser>();

        // Anonymous endpoints are rate limited per IP: without it, login is an open
        // password-guessing oracle and register an unbounded account-creation endpoint.
        groupBuilder.MapPost(Register, "register").RequireRateLimiting(RateLimitPolicies.Public);
        groupBuilder.MapPost(Login, "login").RequireRateLimiting(RateLimitPolicies.Public);
        groupBuilder.MapGet(Info, "info").RequireAuthorization();
        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();

        // --- Administration ---
        groupBuilder.MapGet(GetUsers)
            .RequireAuthorization($"Permission:{DomainPermissions.Users.View}");
        groupBuilder.MapGet(GetUser, "{id}")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.View}");
        groupBuilder.MapPost(CreateUser)
            .RequireAuthorization($"Permission:{DomainPermissions.Users.Create}");
        groupBuilder.MapPut(UpdateUser, "{id}")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.Update}");
        groupBuilder.MapPut(SetUserRoles, "{id}/roles")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.ManageRoles}");
        groupBuilder.MapPut(SetUserPassword, "{id}/password")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.Update}");
        groupBuilder.MapPut(SetUserActive, "{id}/active")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.Update}");
        groupBuilder.MapDelete(DeleteUser, "{id}")
            .RequireAuthorization($"Permission:{DomainPermissions.Users.Delete}");
    }

    [EndpointSummary("Register")]
    [EndpointDescription("Creates a new account with a distinct username and email address.")]
    public static async Task<Ok> Register(
        [FromBody] RegisterUserRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(ToValidationFailures(result.Errors));
        }

        return TypedResults.Ok();
    }

    [EndpointSummary("Log in")]
    [EndpointDescription("Signs in using either a username or an email address, issuing the application cookie.")]
    public static async Task<Ok> Login(
        [FromBody] LoginUserRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.FindByNameAsync(request.Login)
                   ?? await userManager.FindByEmailAsync(request.Login);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid login or password.");
        }

        // Issue the application cookie (matches the cookie behaviour of the built-in /login endpoint).
        signInManager.AuthenticationScheme = IdentityConstants.ApplicationScheme;

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: false, lockoutOnFailure: true);

        // A deactivated account is locked out until DateTimeOffset.MaxValue, so say so rather
        // than letting the user retry a password that is in fact correct.
        if (result.IsLockedOut)
        {
            throw new UnauthorizedAccessException("This account is locked. Contact an administrator.");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid login or password.");
        }

        return TypedResults.Ok();
    }

    [EndpointSummary("Current user")]
    [EndpointDescription("Returns the currently authenticated user. Used by the client to probe the session.")]
    public static async Task<Ok<UserInfoResponse>> Info(
        ClaimsPrincipal claimsPrincipal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }

        var permissions = claimsPrincipal
            .FindAll(DomainPermissions.ClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var roles = claimsPrincipal
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();

        return TypedResults.Ok(
            new UserInfoResponse(user.Id, user.UserName, user.Email, user.DisplayName, roles, permissions));
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Logout(SignInManager<ApplicationUser> signInManager, [FromBody] object empty)
    {
        if (empty != null)
        {
            await signInManager.SignOutAsync();
            return TypedResults.Ok();
        }

        return TypedResults.Unauthorized();
    }

    // --- Administration ---

    [EndpointSummary("List users")]
    [EndpointDescription("Paginated user list. The search term matches user name, email or display name, ignoring case and accents.")]
    public static async Task<Ok<PaginatedList<UserDto>>> GetUsers(ISender sender, [AsParameters] GetUsersQuery query)
        => TypedResults.Ok(await sender.Send(query));

    [EndpointSummary("Get a user")]
    public static async Task<Ok<UserDto>> GetUser(ISender sender, string id)
        => TypedResults.Ok(await sender.Send(new GetUserQuery(id)));

    [EndpointSummary("Create a user")]
    public static async Task<Created<string>> CreateUser(ISender sender, [FromBody] CreateUserCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/Users/{id}", id);
    }

    [EndpointSummary("Update a user")]
    public static async Task<NoContent> UpdateUser(ISender sender, string id, [FromBody] UpdateUserCommand command)
    {
        await sender.Send(command with { UserId = id });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Set a user's roles")]
    [EndpointDescription("Replaces the user's roles. Their session is refreshed so the new permissions take effect.")]
    public static async Task<NoContent> SetUserRoles(ISender sender, string id, [FromBody] SetUserRolesCommand command)
    {
        await sender.Send(command with { UserId = id });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Reset a user's password")]
    [EndpointDescription("Administrative reset: sets a new password without requiring the current one.")]
    public static async Task<NoContent> SetUserPassword(ISender sender, string id, [FromBody] SetUserPasswordCommand command)
    {
        await sender.Send(command with { UserId = id });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Activate or deactivate a user")]
    [EndpointDescription("Deactivating locks the account out indefinitely rather than deleting it.")]
    public static async Task<NoContent> SetUserActive(ISender sender, string id, [FromBody] SetUserActiveRequest request)
    {
        await sender.Send(new SetUserActiveCommand(id, request.IsActive));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete a user")]
    public static async Task<NoContent> DeleteUser(ISender sender, string id)
    {
        await sender.Send(new DeleteUserCommand(id));

        return TypedResults.NoContent();
    }

    // Maps ASP.NET Identity error codes onto the request property they relate to, so the client
    // receives field-level validation errors (camelCased by ValidationException).
    private static IEnumerable<ValidationFailure> ToValidationFailures(IEnumerable<IdentityError> errors) =>
        errors.Select(e => new ValidationFailure(MapErrorCodeToProperty(e.Code), e.Description));

    private static string MapErrorCodeToProperty(string code)
    {
        if (code.Contains("Password", StringComparison.OrdinalIgnoreCase))
            return nameof(RegisterUserRequest.Password);
        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase))
            return nameof(RegisterUserRequest.Email);
        if (code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
            return nameof(RegisterUserRequest.UserName);
        return string.Empty;
    }
}

public record RegisterUserRequest(string UserName, string Email, string Password);

public record LoginUserRequest(string Login, string Password);

public record SetUserActiveRequest(bool IsActive);

public record UserInfoResponse(
    string UserId,
    string? UserName,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
