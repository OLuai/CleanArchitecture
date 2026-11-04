using System.Text.Json.Serialization;

namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Comment la connexion au relais est protégée.
/// <para>
/// Trois valeurs explicites plutôt qu'un « négocier si disponible » : une négociation
/// opportuniste retombe silencieusement en clair quand le serveur n'annonce pas STARTTLS, et ce
/// sont des liens de réinitialisation de mot de passe qui passent dans ces messages. Le choix se
/// fait, il ne se devine pas.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SmtpSecurity>))]
public enum SmtpSecurity
{
    /// <summary>
    /// Aucun chiffrement. Réservé à un relais joignable uniquement depuis le réseau privé — sur
    /// Internet, identifiants et contenu circulent en clair.
    /// </summary>
    None = 0,

    /// <summary>Connexion en clair puis passage en TLS. Le port 587 en pratique.</summary>
    StartTls = 1,

    /// <summary>TLS dès la connexion. Le port 465.</summary>
    Tls = 2
}
