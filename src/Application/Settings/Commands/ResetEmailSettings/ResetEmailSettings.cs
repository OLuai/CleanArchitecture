using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Settings.Commands.ResetEmailSettings;

/// <summary>
/// Removes the saved settings, so that the deployment's settings become the ones that
/// apply again.
/// <para>
/// This is the way out for the day a value typed in the screen silently masks updated
/// environment variables: without it, the database would have to be edited by hand.
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
