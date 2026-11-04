using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Common.Models;
/// <summary>
/// Base record for paginated queries. List queries derive from it and add their own filters.
/// <para>
/// <see cref="PageNumber"/> and <see cref="PageSize"/> are nullable on purpose. Minimal APIs bind
/// these through <c>[AsParameters]</c>, and a non-nullable value type there is a *required* query
/// parameter: <c>GET /api/Projects</c> with no query string answered 500 ("Required parameter
/// int PageNumber was not provided") before any of this record's normalisation could run. It
/// went unnoticed because the client always sends both.
/// </para>
/// <para>
/// Handlers read <see cref="Page"/> and <see cref="Size"/>, which are always sane: absent,
/// negative and oversized values are all clamped rather than rejected.
/// </para>
/// </summary>
public abstract record PaginatedQuery
{
    /// <summary>Override in a derived query that needs a different upper bound.</summary>
    protected virtual int MaxAllowedPageSize => PagingDefaults.MaxPageSize;

    /// <summary>Requested page, or absent for the first one. Handlers use <see cref="Page"/>.</summary>
    public int? PageNumber { get; init; }

    /// <summary>Requested page size, or absent for the default. Handlers use <see cref="Size"/>.</summary>
    public int? PageSize { get; init; }

    /// <summary>The page to read: the requested one, or the first.</summary>
    public int Page => PageNumber is null or < 1 ? PagingDefaults.DefaultPageNumber : PageNumber.Value;

    /// <summary>How many rows to read: clamped to the query's upper bound.</summary>
    public int Size => PageSize switch
    {
        null or < 1 => PagingDefaults.DefaultPageSize,
        _ => Math.Min(PageSize.Value, MaxAllowedPageSize)
    };

    /// <summary>
    /// Sort key (DTO property name, case-insensitive). An unknown or absent key falls back to the
    /// handler's default ordering: see <see cref="Common.Models.SortMap{T}"/>.
    /// </summary>
    public string? SortBy { get; init; }

    public bool? SortDescending { get; init; }
}
