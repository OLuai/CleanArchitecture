namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Chiffre une valeur sensible destinée à être rangée en base — aujourd'hui le mot de passe du
/// relais SMTP.
/// <para>
/// Repose sur l'anneau de clés d'ASP.NET Data Protection, lui-même persisté en base : le secret
/// survit donc aux redéploiements et reste lisible par toutes les instances, ce qu'une clé tirée
/// au démarrage ne permettrait pas.
/// </para>
/// <para>
/// <see cref="Unprotect"/> n'a qu'un seul appelant légitime, l'expéditeur d'e-mails au moment de
/// se connecter au relais. Aucun DTO ne porte de valeur déchiffrée.
/// </para>
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <summary>
    /// Renvoie <c>null</c> si la valeur ne peut pas être déchiffrée — anneau de clés perdu,
    /// donnée abîmée. Un envoi échoue alors proprement au lieu de lever au démarrage.
    /// </summary>
    string? Unprotect(string protectedValue);
}
