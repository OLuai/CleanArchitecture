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
/// Sends over SMTP, with MailKit — <c>System.Net.Mail.SmtpClient</c> is discouraged for new code
/// by its own documentation.
/// <para>
/// The settings are re-read on every send: the saved row if there is one, otherwise the
/// deployment's configuration. A correction made from the administration screen therefore applies
/// to the next message, without a restart — which is the whole point of being able to enter them.
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

            // The mode is the one that was chosen, never negotiated: MailKit does offer a
            // "StartTls if available", which silently falls back to plain text when the server does not
            // advertise it. Neither StartTls nor SslOnConnect tolerates an invalid certificate.
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
            // The recipient is not logged, and neither is the message. The relay's error does
            // go back up to the caller, though: it is what says which field to fix.
            _logger.LogWarning(ex, "SMTP send failed against {Host}:{Port}", settings.Host, settings.Port);

            return EmailResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// The settings that apply, with the password decrypted, or <c>null</c> if no server
    /// is specified anywhere.
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
