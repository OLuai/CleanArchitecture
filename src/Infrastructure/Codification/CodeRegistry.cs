using System.Collections.Concurrent;

namespace CleanArchitecture.Infrastructure.Codification;

public sealed class CodeRegistry
{
    private readonly ConcurrentDictionary<Type, CodeRegistration> _registrations = new();

    public CodeRegistry(IEnumerable<CodeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (!_registrations.TryAdd(registration.EntityType, registration))
            {
                throw new InvalidOperationException(
                    $"A code radical is already registered for type '{registration.EntityType.FullName}'.");
            }
        }
    }

    public bool TryGet(Type entityType, out CodeRegistration registration)
    {
        return _registrations.TryGetValue(entityType, out registration!);
    }

    public IEnumerable<CodeRegistration> All => _registrations.Values;
}
