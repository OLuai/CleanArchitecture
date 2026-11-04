namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Les réglages du relais SMTP, saisis depuis l'écran d'administration.
/// <para>
/// Une seule ligne, garantie par une clé primaire constante : deux configurations d'envoi
/// simultanées n'auraient aucun sens, et un « prendre la première » se serait mis à dépendre de
/// l'ordre d'insertion le jour où une deuxième aurait été créée par accident.
/// </para>
/// <para>
/// Ces réglages <b>surchargent</b> ceux de la configuration (section <c>Smtp</c>, variables
/// d'environnement <c>Smtp__*</c>). Sans ligne ici, c'est l'environnement qui s'applique — un
/// déploiement fonctionne donc sans que personne n'ouvre l'écran, et un administrateur peut
/// corriger un réglage sans redéployer.
/// </para>
/// </summary>
public class EmailSettings : BaseAuditableEntity<Guid>
{
    /// <summary>
    /// Clé unique de l'unique ligne. Constante et non générée : c'est elle qui rend la table
    /// singleton, sans colonne technique ni index à expression.
    /// </summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public string Host { get; set; } = null!;

    public int Port { get; set; }

    public SmtpSecurity Security { get; set; }

    /// <summary>Nul pour un relais qui n'exige pas d'authentification.</summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Mot de passe chiffré par <c>ISecretProtector</c>, jamais en clair en base et jamais
    /// renvoyé au client. Nul quand le relais n'en demande pas.
    /// </summary>
    public string? ProtectedPassword { get; set; }

    /// <summary>Adresse d'expédition. Le relais refuse souvent d'en servir une autre.</summary>
    public string FromAddress { get; set; } = null!;

    /// <summary>Nom affiché à côté de l'adresse : « CleanArchitecture ».</summary>
    public string? FromName { get; set; }
}
