using System.Security.Claims;

using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public List<string>? Roles => _httpContextAccessor.HttpContext?.User?
        .FindAll(ClaimTypes.Role)
        .Select(x => x.Value)
        .ToList();

    public IReadOnlyCollection<string>? Permissions => _httpContextAccessor.HttpContext?.User?
        .FindAll(Domain.Constants.Permissions.ClaimType)
        .Select(c => c.Value)
        .ToArray();
}
