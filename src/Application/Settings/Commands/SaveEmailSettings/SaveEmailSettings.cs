using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Settings.Commands.SaveEmailSettings;

/// <summary>
/// Saves the relay settings, overriding the deployment's.
/// </summary>
[Authorize(Permissions = Permissions.Settings.ManageEmail)]
public record SaveEmailSettingsCommand : IRequest
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string? UserName { get; init; }

    /// <summary>
    /// <c>null</c> leaves the saved password untouched — which is what the screen sends when
    /// the field is not touched, since it cannot pre-fill it: it never received it.
    /// Any value, even an empty one, replaces it; an empty string therefore removes the password, which
    /// is what internal relays that do not require one expect.
    /// </summary>
    public string? Password { get; init; }

    public required string FromAddress { get; init; }

    public string? FromName { get; init; }
}

public class SaveEmailSettingsCommandHandler : IRequestHandler<SaveEmailSettingsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISecretProtector _protector;

    public SaveEmailSettingsCommandHandler(IApplicationDbContext context, ISecretProtector protector)
    {
        _context = context;
        _protector = protector;
    }

    public async Task Handle(SaveEmailSettingsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EmailSettings
            .FirstOrDefaultAsync(s => s.Id == EmailSettings.SingletonId, cancellationToken);

        if (entity is null)
        {
            // Constant key: it is what forbids a second configuration, even under
            // two simultaneous inserts — the second one fails on the primary key.
            entity = new EmailSettings { Id = EmailSettings.SingletonId };
            _context.EmailSettings.Add(entity);
        }

        entity.Host = request.Host.Trim();
        entity.Port = request.Port;
        entity.Security = request.Security;
        entity.UserName = string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim();
        entity.FromAddress = request.FromAddress.Trim();
        entity.FromName = string.IsNullOrWhiteSpace(request.FromName) ? null : request.FromName.Trim();

        if (request.Password is not null)
        {
            entity.ProtectedPassword = request.Password.Length == 0
                ? null
                : _protector.Protect(request.Password);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
