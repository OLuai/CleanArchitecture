using System.Text.Encodings.Web;
using CleanArchitecture.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ApplicationEmailSender = CleanArchitecture.Application.Common.Interfaces.IEmailSender;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// Plugs ASP.NET Identity's messages — address confirmation, password
/// reset — into the configured relay.
/// <para>
/// Without this adapter, <c>AddApiEndpoints()</c> registers a sender that does nothing: the
/// endpoints answered 200, the screen displayed "a confirmation link has been
/// sent to you", and nothing was ever sent.
/// </para>
/// <para>
/// Singleton, with a scope opened on every send: <c>MapIdentityApi</c> resolves this sender
/// from the root container, when the routes are declared. Declaring it <c>Scoped</c>
/// makes container validation fail at startup — the real sender, for its part, depends on the
/// <c>DbContext</c> and is therefore resolved per call.
/// </para>
/// </summary>
public sealed class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IdentityEmailSender(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(
            email,
            "CleanArchitecture — confirm your email address",
            $"""
             <p>Hello{Greeting(user)},</p>
             <p>Confirm this address to link it to your CleanArchitecture account:</p>
             <p><a href="{HtmlEncoder.Default.Encode(confirmationLink)}">Confirm my email address</a></p>
             <p>If you did not make this request, you can ignore this message.</p>
             """);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(
            email,
            "CleanArchitecture — reset your password",
            $"""
             <p>Hello{Greeting(user)},</p>
             <p>You can set a new password:</p>
             <p><a href="{HtmlEncoder.Default.Encode(resetLink)}">Choose a new password</a></p>
             <p>If you did not make this request, you can ignore this message: your current
             password remains valid.</p>
             """);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(
            email,
            "CleanArchitecture — password reset code",
            $"""
             <p>Hello{Greeting(user)},</p>
             <p>Your password reset code: <strong>{HtmlEncoder.Default.Encode(resetCode)}</strong></p>
             <p>If you did not make this request, you can ignore this message.</p>
             """);

    /// <summary>
    /// Identity expects a result-less <c>Task</c> and cannot do anything with a failure. The error is
    /// therefore swallowed here — the sender has already logged it — rather than making
    /// a sign-up fail because the relay is unavailable.
    /// </summary>
    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var scope = _scopeFactory.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ApplicationEmailSender>();

        await sender.SendAsync(to, subject, htmlBody, CancellationToken.None);
    }

    private static string Greeting(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.DisplayName) ? string.Empty : $" {user.DisplayName}";
}
