namespace CleanArchitecture.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<BaseEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
