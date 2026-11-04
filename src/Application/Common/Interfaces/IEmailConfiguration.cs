using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Les réglages SMTP tels que le déploiement les fournit : section <c>Smtp</c> de la
/// configuration, donc en pratique les variables d'environnement <c>Smtp__Host</c>,
/// <c>Smtp__Port</c>, <c>Smtp__Security</c>, <c>Smtp__UserName</c>, <c>Smtp__Password</c>,
/// <c>Smtp__FromAddress</c>…
/// <para>
/// C'est le socle : une plateforme démarre configurée sans que personne n'ouvre l'écran
/// d'administration. Une ligne <c>EmailSettings</c> en base le surcharge ensuite.
/// </para>
/// </summary>
public interface IEmailConfiguration
{
    string? Host { get; }

    int Port { get; }

    SmtpSecurity Security { get; }

    string? UserName { get; }

    /// <summary>
    /// En clair, car il vient de l'environnement. Il ne sort jamais par l'API : le DTO ne porte
    /// qu'un booléen disant qu'il existe.
    /// </summary>
    string? Password { get; }

    string? FromAddress { get; }

    string? FromName { get; }
}
