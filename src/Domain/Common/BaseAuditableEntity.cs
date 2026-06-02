namespace CleanArchitecture.Domain.Common;

public abstract class BaseAuditableEntity<TKey> : BaseEntity<TKey>, IAuditableEntity
    where TKey : notnull
{
    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}

public abstract class BaseAuditableEntity : BaseAuditableEntity<long>
{
}
