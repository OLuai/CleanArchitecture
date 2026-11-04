namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// The SMTP relay settings, entered from the administration screen.
/// <para>
/// A single row, guaranteed by a constant primary key: two simultaneous sending configurations
/// would make no sense, and a "take the first one" would have come to depend on
/// insertion order the day a second one was created by accident.
/// </para>
/// <para>
/// These settings <b>override</b> those of the configuration (<c>Smtp</c> section, <c>Smtp__*</c>
/// environment variables). With no row here, the environment applies — a
/// deployment therefore works without anyone opening the screen, and an administrator can
/// fix a setting without redeploying.
/// </para>
/// </summary>
public class EmailSettings : BaseAuditableEntity<Guid>
{
    /// <summary>
    /// Unique key of the single row. Constant rather than generated: it is what makes the table a
    /// singleton, with no technical column or expression index.
    /// </summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public string Host { get; set; } = null!;

    public int Port { get; set; }

    public SmtpSecurity Security { get; set; }

    /// <summary>Null for a relay that does not require authentication.</summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Password encrypted by <c>ISecretProtector</c>, never in plain text in the database and never
    /// returned to the client. Null when the relay does not require one.
    /// </summary>
    public string? ProtectedPassword { get; set; }

    /// <summary>Sender address. The relay often refuses to send from any other.</summary>
    public string FromAddress { get; set; } = null!;

    /// <summary>Name displayed next to the address: "CleanArchitecture".</summary>
    public string? FromName { get; set; }
}
