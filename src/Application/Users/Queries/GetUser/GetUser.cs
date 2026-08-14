using NotFoundException = CleanArchitecture.Application.Common.Exceptions.NotFoundException;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Users.Queries.GetUser;

[Authorize(Permissions = Permissions.Users.View)]
public record GetUserQuery(string UserId) : IRequest<UserDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IIdentityService _identityService;

    public GetUserQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => await _identityService.GetUserAsync(request.UserId, cancellationToken)
           ?? throw new NotFoundException(request.UserId, nameof(UserDto));
}
