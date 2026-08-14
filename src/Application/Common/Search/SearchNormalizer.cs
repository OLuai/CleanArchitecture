using System.Globalization;
using System.Text;

namespace CleanArchitecture.Application.Common.Search;

/// <summary>
/// Application-side normalisation of search terms: accent folding (NFD decomposition, then
/// removal of combining marks) and LIKE wildcard escaping. The normalised term is consumed by
/// <c>EF.Functions.Like</c> against a column that PostgreSQL strips of accents through the
/// <c>f_unaccent</c> SQL function — see the AddAccentInsensitiveSearch migration.
/// </summary>
public static class SearchNormalizer
{
    /// <summary>
    /// Folds accents by decomposing to NFD and dropping combining diacritics. The C# equivalent of
    /// PostgreSQL's <c>unaccent</c> for common Latin characters.
    /// </summary>
    public static string FoldAccents(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes LIKE metacharacters, so a % or _ typed by the user does not become a wildcard.
    /// PostgreSQL's default escape character is a backslash.
    /// </summary>
    public static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// Prepares a search term: trim, fold accents, lowercase, escape, and wrap in % wildcards.
    /// Returns null for an empty or whitespace term, meaning "no filter to apply".
    /// </summary>
    public static string? PreparePattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        return "%" + EscapeLike(FoldAccents(search.Trim()).ToLowerInvariant()) + "%";
    }
}
