using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// Réglages SMTP fournis par le déploiement. Section <c>Smtp</c>, donc en variables
/// d'environnement : <c>Smtp__Host</c>, <c>Smtp__Port</c>, <c>Smtp__Security</c>,
/// <c>Smtp__UserName</c>, <c>Smtp__Password</c>, <c>Smtp__FromAddress</c>, <c>Smtp__FromName</c>.
/// </summary>
public class SmtpOptions : IEmailConfiguration
{
    public const string Section = "Smtp";

    public string? Host { get; set; }

    /// <summary>587 par défaut : le port de la soumission avec STARTTLS.</summary>
    public int Port { get; set; } = 587;

    /// <summary><c>StartTls</c>, <c>Tls</c> ou <c>None</c>, littéralement.</summary>
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromName { get; set; } = "CleanArchitecture";

    /// <summary>Au-delà, on renonce plutôt que de laisser une requête attendre un relais muet.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
