using System.Text.Encodings.Web;
using CleanArchitecture.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ApplicationEmailSender = CleanArchitecture.Application.Common.Interfaces.IEmailSender;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// Branche les messages d'ASP.NET Identity — confirmation d'adresse, réinitialisation de mot de
/// passe — sur le relais configuré.
/// <para>
/// Sans cet adaptateur, <c>AddApiEndpoints()</c> enregistre un expéditeur qui ne fait rien : les
/// points d'entrée répondaient 200, l'écran affichait « un lien de confirmation vous a été
/// envoyé », et rien ne partait jamais.
/// </para>
/// <para>
/// Singleton, et une portée ouverte à chaque envoi : <c>MapIdentityApi</c> résout cet expéditeur
/// depuis le conteneur racine, au moment où les routes sont déclarées. Le déclarer <c>Scoped</c>
/// fait échouer la validation du conteneur au démarrage — l'expéditeur réel, lui, dépend du
/// <c>DbContext</c> et reste donc résolu par appel.
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
    /// Identity attend un <c>Task</c> sans résultat et ne sait rien faire d'un échec. L'erreur est
    /// donc absorbée ici — elle est déjà journalisée par l'expéditeur — plutôt que de faire
    /// échouer une inscription parce que le relais est indisponible.
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
