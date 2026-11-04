using System.Text.Json.Serialization;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Settings.Queries.GetEmailSettings;

/// <summary>D'où viennent les réglages qui s'appliquent réellement.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmailSettingsSource>))]
public enum EmailSettingsSource
{
    /// <summary>Aucun relais configuré : rien n'est envoyé.</summary>
    None = 0,

    /// <summary>La configuration du déploiement — section <c>Smtp</c>, variables <c>Smtp__*</c>.</summary>
    Environment = 1,

    /// <summary>Une ligne enregistrée depuis cet écran, qui surcharge l'environnement.</summary>
    Database = 2
}

/// <summary>
/// Les réglages d'envoi tels qu'ils s'appliquent, et d'où ils viennent.
/// <para>
/// <b>Aucun mot de passe n'y figure</b>, ni chiffré ni en clair : seulement le fait qu'il y en ait
/// un. Un réglage qu'on peut saisir n'a pas à pouvoir être relu.
/// </para>
/// </summary>
public record EmailSettingsDto
{
    public required EmailSettingsSource Source { get; init; }

    public string? Host { get; init; }

    public int? Port { get; init; }

    public SmtpSecurity Security { get; init; }

    public string? UserName { get; init; }

    /// <summary>Un mot de passe est enregistré. Sa valeur ne sort jamais.</summary>
    public bool HasPassword { get; init; }

    public string? FromAddress { get; init; }

    public string? FromName { get; init; }
}

[Authorize(Permissions = Permissions.Settings.ManageEmail)]
public record GetEmailSettingsQuery : IRequest<EmailSettingsDto>;

public class GetEmailSettingsQueryHandler : IRequestHandler<GetEmailSettingsQuery, EmailSettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailConfiguration _configured;

    public GetEmailSettingsQueryHandler(IApplicationDbContext context, IEmailConfiguration configured)
    {
        _context = context;
        _configured = configured;
    }

    public async Task<EmailSettingsDto> Handle(
        GetEmailSettingsQuery request, CancellationToken cancellationToken)
    {
        var saved = await _context.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == EmailSettings.SingletonId, cancellationToken);

        if (saved is not null)
        {
            return new EmailSettingsDto
            {
                Source = EmailSettingsSource.Database,
                Host = saved.Host,
                Port = saved.Port,
                Security = saved.Security,
                UserName = saved.UserName,
                HasPassword = !string.IsNullOrEmpty(saved.ProtectedPassword),
                FromAddress = saved.FromAddress,
                FromName = saved.FromName
            };
        }

        // Rien en base : c'est le déploiement qui décide. L'écran le dit, pour qu'un
        // administrateur sache qu'il regarde des valeurs venues d'ailleurs.
        if (string.IsNullOrWhiteSpace(_configured.Host))
        {
            // Rien nulle part : on renvoie tout de même un point de départ plausible plutôt que
            // les valeurs par défaut des types. `SmtpSecurity.None` étant la valeur zéro, un
            // formulaire vierge proposerait sinon « aucun chiffrement » — le pire des trois choix
            // à afficher par défaut.
            return new EmailSettingsDto
            {
                Source = EmailSettingsSource.None,
                Port = 587,
                Security = SmtpSecurity.StartTls
            };
        }

        return new EmailSettingsDto
        {
            Source = EmailSettingsSource.Environment,
            Host = _configured.Host,
            Port = _configured.Port,
            Security = _configured.Security,
            UserName = _configured.UserName,
            HasPassword = !string.IsNullOrEmpty(_configured.Password),
            FromAddress = _configured.FromAddress,
            FromName = _configured.FromName
        };
    }
}
