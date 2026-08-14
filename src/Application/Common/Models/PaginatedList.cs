namespace CleanArchitecture.Application.Common.Models;

/// <summary>
/// Generic paginated result returned by list queries. Build it with <see cref="CreateAsync"/>
/// over an EF <see cref="IQueryable{T}"/> (Count then Skip/Take).
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>Current page, 1-based.</summary>
    public int PageNumber { get; }

    /// <summary>Page size actually applied.</summary>
    public int PageSize { get; }

    /// <summary>Total number of items across every page.</summary>
    public int TotalCount { get; }

    public int TotalPages { get; }

    /// <summary>
    /// True when there is a page to go back to. One page past the end still counts (page 11 of 10
    /// can return to page 10); two or more pages past the end does not.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1 && PageNumber <= TotalPages + 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
