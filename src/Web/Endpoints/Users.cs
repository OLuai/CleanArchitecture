using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Infrastructure.Identity;
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

        groupBuilder.MapPost(Register, "register");
        groupBuilder.MapPost(Login, "login");
        groupBuilder.MapGet(Info, "info").RequireAuthorization();
        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
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
            .FindAll(CleanArchitecture.Domain.Constants.Permissions.ClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        return TypedResults.Ok(new UserInfoResponse(user.Id, user.UserName, user.Email, permissions));
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

public record UserInfoResponse(string UserId, string? UserName, string? Email, IReadOnlyList<string> Permissions);
