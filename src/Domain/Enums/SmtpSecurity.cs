using System.Text.Json.Serialization;

namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// How the connection to the relay is protected.
/// <para>
/// Three explicit values rather than a "negotiate if available": opportunistic negotiation
/// silently falls back to plain text when the server does not advertise STARTTLS, and it is
/// password reset links that travel in these messages. The choice is made, not guessed.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SmtpSecurity>))]
public enum SmtpSecurity
{
    /// <summary>
    /// No encryption. Reserved for a relay reachable only from the private network — on the
    /// Internet, credentials and content travel in plain text.
    /// </summary>
    None = 0,

    /// <summary>Plain-text connection, then upgraded to TLS. Port 587 in practice.</summary>
    StartTls = 1,

    /// <summary>TLS from the start of the connection. Port 465.</summary>
    Tls = 2
}
