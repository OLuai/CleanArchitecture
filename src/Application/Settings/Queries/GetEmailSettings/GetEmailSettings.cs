using System.Text.Json.Serialization;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Settings.Queries.GetEmailSettings;

/// <summary>Where the settings that actually apply come from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmailSettingsSource>))]
public enum EmailSettingsSource
{
    /// <summary>No relay configured: nothing is sent.</summary>
    None = 0,

    /// <summary>The deployment's configuration — <c>Smtp</c> section, <c>Smtp__*</c> variables.</summary>
    Environment = 1,

    /// <summary>A row saved from this screen, which overrides the environment.</summary>
    Database = 2
}

/// <summary>
/// The sending settings as they apply, and where they come from.
/// <para>
/// <b>No password appears here</b>, neither encrypted nor in plain text: only the fact that there is
/// one. A setting that can be typed in does not need to be readable back.
/// </para>
/// </summary>
public record EmailSettingsDto
{
    public required EmailSettingsSource Source { get; init; }

    public string? Host { get; init; }

    public int? Port { get; init; }

    public SmtpSecurity Security { get; init; }

    public string? UserName { get; init; }

    /// <summary>A password is saved. Its value never leaves.</summary>
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

        // Nothing in the database: the deployment decides. The screen says so, so that an
        // administrator knows they are looking at values that came from elsewhere.
        if (string.IsNullOrWhiteSpace(_configured.Host))
        {
            // Nothing anywhere: still return a plausible starting point rather than
            // the types' default values. Since `SmtpSecurity.None` is the zero value, a
            // blank form would otherwise offer "no encryption" — the worst of the three choices
            // to show by default.
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
