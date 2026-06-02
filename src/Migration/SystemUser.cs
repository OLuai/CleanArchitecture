using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Migration;

/// <summary>
/// IUser implementation used by the Migration worker. There is no authenticated user during
/// schema migration and seeding, so audit fields written by AuditableEntityInterceptor will
/// be left null (CreatedBy/LastModifiedBy on BaseAuditableEntity are nullable).
/// </summary>
public class SystemUser : IUser
{
    public string? Id => null;
    public List<string>? Roles => null;
    public IReadOnlyCollection<string>? Permissions => null;
}
