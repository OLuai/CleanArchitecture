namespace CleanArchitecture.Domain.Constants;

/// <summary>
/// Defaults and bounds for server-side pagination, shared by paginated queries and their
/// validators. The upper bound protects the database from unbounded page sizes.
/// </summary>
public static class PagingDefaults
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
