namespace CleanArchitecture.Domain.Common;

/// <summary>
/// Entité porteuse d'un code métier lisible, distinct de sa clé primaire.
/// <para>
/// La clé est un <see cref="Guid"/> : elle n'a de sens que pour la base et les relations, elle se
/// génère sans aller-retour et elle ne fuit aucune information sur le volume de données. Le code
/// (<c>PRJ-000123</c>) est ce qu'un humain lit, dicte au téléphone et retrouve dans un ticket ;
/// il est attribué séquentiellement par <c>HiLoCodeGenerator</c> à l'insertion.
/// </para>
/// <para>
/// Séparer les deux évite d'avoir à choisir : une clé technique stable d'un côté, une référence
/// métier lisible et unique de l'autre.
/// </para>
/// </summary>
public interface IHasCode
{
    string Code { get; set; }
}
