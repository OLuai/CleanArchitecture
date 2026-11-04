using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Settings.Commands.ResetEmailSettings;

/// <summary>
/// Retire les réglages enregistrés, de sorte que ceux du déploiement redeviennent ceux qui
/// s'appliquent.
/// <para>
/// C'est la porte de sortie du jour où une saisie depuis l'écran masque silencieusement des
/// variables d'environnement mises à jour : sans elle, il faudrait toucher la base à la main.
/// </para>
/// </summary>
[Authorize(Permissions = Permissions.Settings.ManageEmail)]
public record ResetEmailSettingsCommand : IRequest;

public class ResetEmailSettingsCommandHandler : IRequestHandler<ResetEmailSettingsCommand>
{
    private readonly IApplicationDbContext _context;

    public ResetEmailSettingsCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(ResetEmailSettingsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EmailSettings
            .FirstOrDefaultAsync(s => s.Id == EmailSettings.SingletonId, cancellationToken);

        if (entity is null) return;

        _context.EmailSettings.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
