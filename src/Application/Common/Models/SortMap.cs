using System.Linq.Expressions;

namespace CleanArchitecture.Application.Common.Models;

/// <summary>
/// Per-query allow-list of sort keys: a key (DTO field name, case-insensitive) maps to an
/// expression over the entity, applied before projection and pagination. Unknown or absent keys
/// fall back to the handler's default ordering. Because the keys are declared up front, no
/// caller-supplied string ever reaches the query — there is no injection surface and no
/// reflection. Chain a <c>ThenBy(x =&gt; x.Id)</c> after <see cref="Apply"/> when the sort key is
/// not unique, so paging stays deterministic.
/// </summary>
public sealed class SortMap<T> where T : class
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _map
        = new(StringComparer.OrdinalIgnoreCase);

    public SortMap<T> Add<TKey>(string key, Expression<Func<T, TKey>> selector)
    {
        _map[key] = (q, desc) => desc ? q.OrderByDescending(selector) : q.OrderBy(selector);
        return this;
    }

    public IOrderedQueryable<T> Apply(
        IQueryable<T> source,
        string? sortBy,
        bool? descending,
        Func<IQueryable<T>, IOrderedQueryable<T>> defaultSort)
        => sortBy is not null && _map.TryGetValue(sortBy, out var sort)
            ? sort(source, descending == true)
            : defaultSort(source);
}
