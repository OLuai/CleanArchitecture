namespace CleanArchitecture.Application.Common.Search;

/// <summary>
/// PostgreSQL search helpers. <see cref="Unaccent"/> is mapped to the <c>f_unaccent</c> SQL
/// function (an IMMUTABLE wrapper around the <c>unaccent</c> extension, created by the
/// AddAccentInsensitiveSearch migration). The mapping is registered in
/// <c>ApplicationDbContext.OnModelCreating</c>.
/// <para>
/// Typical use in a handler:
/// <code>
/// var pattern = SearchNormalizer.PreparePattern(request.Search);
/// query.Where(x => pattern == null
///     || EF.Functions.Like(DbSearchExtensions.Unaccent(x.Name).ToLower(), pattern))
/// </code>
/// </para>
/// <para>
/// <c>lower(f_unaccent(col)) LIKE pattern</c> handles case and accents; the pattern is
/// pre-normalised in C# (fold, lowercase, escape). A trigram GIN index on
/// <c>lower(f_unaccent(col))</c> keeps substring search off a sequential scan.
/// </para>
/// </summary>
public static class DbSearchExtensions
{
    /// <summary>
    /// Stub whose body is replaced by <c>f_unaccent</c> during LINQ-to-SQL translation. Outside
    /// PostgreSQL (unit tests, in-memory providers) it falls back to folding accents in C#.
    /// </summary>
    public static string Unaccent(string text) => SearchNormalizer.FoldAccents(text);
}
