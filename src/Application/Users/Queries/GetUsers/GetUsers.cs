using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Queries.GetUsers;

/// <summary>
/// Paginated user list for the administration screen. <paramref name="Search"/> matches the user
/// name, email or display name, ignoring case and accents.
/// </summary>
[Authorize(Permissions = Permissions.Users.View)]
public record GetUsersQuery : PaginatedQuery, IRequest<PaginatedList<UserDto>>
{
    public string? Search { get; init; }

    public string? Role { get; init; }

    public bool? IsActive { get; init; }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        => _identityService.GetUsersAsync(
            new UserSearch
            {
                Search = request.Search,
                Role = request.Role,
                IsActive = request.IsActive,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            },
            cancellationToken);
}
