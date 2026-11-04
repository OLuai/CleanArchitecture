using System.Collections.Concurrent;

namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed class EntityCodeRegistry
{
    private readonly ConcurrentDictionary<Type, EntityCodeRegistration> _registrations = new();

    public EntityCodeRegistry(IEnumerable<EntityCodeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (!_registrations.TryAdd(registration.EntityType, registration))
            {
                throw new InvalidOperationException(
                    $"An entity code radical is already registered for type '{registration.EntityType.FullName}'.");
            }
        }
    }

    public bool TryGet(Type entityType, out EntityCodeRegistration registration)
    {
        return _registrations.TryGetValue(entityType, out registration!);
    }

    public IEnumerable<EntityCodeRegistration> All => _registrations.Values;
}
