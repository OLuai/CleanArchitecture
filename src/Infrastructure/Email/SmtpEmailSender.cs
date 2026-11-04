using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// Envoie par SMTP, avec MailKit — <c>System.Net.Mail.SmtpClient</c> est déconseillé pour du code
/// neuf par sa propre documentation.
/// <para>
/// Les réglages sont relus à chaque envoi : la ligne enregistrée si elle existe, sinon la
/// configuration du déploiement. Une correction depuis l'écran d'administration s'applique donc
/// au message suivant, sans redémarrage — c'est tout l'intérêt de pouvoir les saisir.
/// </para>
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretProtector _protector;
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        ApplicationDbContext context,
        ISecretProtector protector,
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _context = context;
        _protector = protector;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
        => await ResolveAsync(cancellationToken) is not null;

    public async Task<EmailResult> SendAsync(
        string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var settings = await ResolveAsync(cancellationToken);

        if (settings is null)
        {
            return EmailResult.Failure("No SMTP relay is configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName ?? string.Empty, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient { Timeout = (int)_options.Timeout.TotalMilliseconds };

            // Le mode est celui qui a été choisi, jamais négocié : MailKit propose bien un
            // « StartTls si disponible », qui retombe en clair sans le dire quand le serveur ne
            // l'annonce pas. Ni StartTls ni SslOnConnect ne tolèrent un certificat invalide.
            var security = settings.Security switch
            {
                SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
                SmtpSecurity.Tls => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.None
            };

            await client.ConnectAsync(settings.Host, settings.Port, security, cancellationToken);

            if (!string.IsNullOrEmpty(settings.UserName))
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            return EmailResult.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                   || !cancellationToken.IsCancellationRequested)
        {
            // Le destinataire n'est pas journalisé, et le message non plus. L'erreur du relais
            // remonte en revanche à l'appelant : c'est elle qui dit quel champ corriger.
            _logger.LogWarning(ex, "SMTP send failed against {Host}:{Port}", settings.Host, settings.Port);

            return EmailResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Les réglages qui s'appliquent, mot de passe déchiffré, ou <c>null</c> si aucun serveur
    /// n'est renseigné nulle part.
    /// </summary>
    private async Task<ResolvedSettings?> ResolveAsync(CancellationToken cancellationToken)
    {
        var saved = await _context.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == EmailSettings.SingletonId, cancellationToken);

        if (saved is not null)
        {
            return new ResolvedSettings(
                saved.Host,
                saved.Port,
                saved.Security,
                saved.UserName,
                saved.ProtectedPassword is null ? null : _protector.Unprotect(saved.ProtectedPassword),
                saved.FromAddress,
                saved.FromName);
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return null;
        }

        return new ResolvedSettings(
            _options.Host,
            _options.Port,
            _options.Security,
            _options.UserName,
            _options.Password,
            _options.FromAddress,
            _options.FromName);
    }

    private sealed record ResolvedSettings(
        string Host,
        int Port,
        SmtpSecurity Security,
        string? UserName,
        string? Password,
        string FromAddress,
        string? FromName);
}
