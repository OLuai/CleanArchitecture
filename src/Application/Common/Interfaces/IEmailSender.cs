namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>The outcome of a send. The error message is passed as-is to the settings screen.</summary>
public record EmailResult(bool Sent, string? Error)
{
    public static EmailResult Success() => new(true, null);

    public static EmailResult Failure(string error) => new(false, error);
}

/// <summary>
/// Sends a message through the configured relay.
/// <para>
/// The settings are resolved on every send — the database row if there is one, otherwise the
/// configuration — so a correction made from the administration screen applies
/// immediately, without a restart.
/// </para>
/// </summary>
public interface IEmailSender
{
    /// <summary>False when no relay is configured, neither in the database nor in the environment.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Never fails with an exception: the result carries the error. An email that cannot be sent must not
    /// bring down the request that triggered it — a sign-up, for example.
    /// </summary>
    Task<EmailResult> SendAsync(
        string to, string subject, string htmlBody, CancellationToken cancellationToken);
}
