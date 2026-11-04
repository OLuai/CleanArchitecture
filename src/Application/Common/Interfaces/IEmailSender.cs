namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>Ce qu'un envoi a donné. Le message d'erreur remonte tel quel à l'écran de réglages.</summary>
public record EmailResult(bool Sent, string? Error)
{
    public static EmailResult Success() => new(true, null);

    public static EmailResult Failure(string error) => new(false, error);
}

/// <summary>
/// Envoi d'un message par le relais configuré.
/// <para>
/// Les réglages sont résolus à chaque envoi — la ligne en base si elle existe, sinon la
/// configuration — de sorte qu'une correction depuis l'écran d'administration s'applique
/// immédiatement, sans redémarrage.
/// </para>
/// </summary>
public interface IEmailSender
{
    /// <summary>Faux quand aucun relais n'est configuré, ni en base ni dans l'environnement.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    /// <summary>
    /// N'échoue jamais par exception : le résultat porte l'erreur. Un e-mail qui ne part pas ne
    /// doit pas faire tomber la requête qui l'a déclenché — une inscription, par exemple.
    /// </summary>
    Task<EmailResult> SendAsync(
        string to, string subject, string htmlBody, CancellationToken cancellationToken);
}
