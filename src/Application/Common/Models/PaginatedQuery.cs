using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Common.Models;

/// <summary>
/// Base record for paginated queries. Normalises <see cref="PageNumber"/> and
/// <see cref="PageSize"/> by clamping them to <see cref="PagingDefaults"/> rather than failing.
/// List queries derive from this record and add their own filters.
/// </summary>
public abstract record PaginatedQuery
{
    private readonly int _pageNumber = PagingDefaults.DefaultPageNumber;
    private readonly int _pageSize = PagingDefaults.DefaultPageSize;

    /// <summary>Override in a derived query that needs a different upper bound.</summary>
    protected virtual int MaxAllowedPageSize => PagingDefaults.MaxPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? PagingDefaults.DefaultPageNumber : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => PagingDefaults.DefaultPageSize,
            _ => Math.Min(value, MaxAllowedPageSize)
        };
    }

    /// <summary>
    /// Sort key (DTO property name, case-insensitive). An unknown or absent key falls back to the
    /// handler's default ordering — see <see cref="SortMap{T}"/>.
    /// </summary>
    public string? SortBy { get; init; }

    public bool? SortDescending { get; init; }
}
