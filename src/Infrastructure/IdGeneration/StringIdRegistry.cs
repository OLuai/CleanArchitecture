using System.Collections.Concurrent;

namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed class StringIdRegistry
{
    private readonly ConcurrentDictionary<Type, StringIdRegistration> _registrations = new();

    public StringIdRegistry(IEnumerable<StringIdRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (!_registrations.TryAdd(registration.EntityType, registration))
            {
                throw new InvalidOperationException(
                    $"A string id radical is already registered for type '{registration.EntityType.FullName}'.");
            }
        }
    }

    public bool TryGet(Type entityType, out StringIdRegistration registration)
    {
        return _registrations.TryGetValue(entityType, out registration!);
    }

    public IEnumerable<StringIdRegistration> All => _registrations.Values;
}
