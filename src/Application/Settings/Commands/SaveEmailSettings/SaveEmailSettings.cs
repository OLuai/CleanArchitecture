using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Settings.Commands.SaveEmailSettings;

/// <summary>
/// Enregistre les réglages du relais, en surcharge de ceux du déploiement.
/// </summary>
[Authorize(Permissions = Permissions.Settings.ManageEmail)]
public record SaveEmailSettingsCommand : IRequest
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string? UserName { get; init; }

    /// <summary>
    /// <c>null</c> laisse le mot de passe enregistré tel quel — c'est ce que fait l'écran quand
    /// on ne touche pas au champ, puisqu'il ne peut pas le pré-remplir : il ne l'a jamais reçu.
    /// Une valeur, même vide, le remplace ; une chaîne vide retire donc le mot de passe, ce
    /// qu'attendent les relais internes qui n'en demandent pas.
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
            // Clé constante : c'est elle qui interdit une deuxième configuration, y compris sous
            // deux enregistrements simultanés — le second échoue sur la clé primaire.
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
