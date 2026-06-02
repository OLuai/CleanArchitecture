using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed class StringIdGenerationInterceptor : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, bool> _isStringEntityCache = new();

    private readonly StringIdRegistry _registry;
    private readonly StringHiLoIdGenerator _generator;

    public StringIdGenerationInterceptor(StringIdRegistry registry, StringHiLoIdGenerator generator)
    {
        _registry = registry;
        _generator = generator;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AssignIdsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await AssignIdsAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task AssignIdsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;

            var entity = entry.Entity;
            var type = entity.GetType();

            if (!IsStringKeyedBaseEntity(type)) continue;
            if (!_registry.TryGet(type, out var registration)) continue;

            var idProperty = entry.Property(nameof(BaseEntity<string>.Id));
            if (idProperty.CurrentValue is string current && !string.IsNullOrEmpty(current)) continue;

            var newId = await _generator.NextAsync(registration, cancellationToken).ConfigureAwait(false);
            idProperty.CurrentValue = newId;
        }
    }

    private static bool IsStringKeyedBaseEntity(Type type)
    {
        return _isStringEntityCache.GetOrAdd(type, static t =>
        {
            var current = t;
            while (current is not null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseEntity<>))
                {
                    return current.GetGenericArguments()[0] == typeof(string);
                }
                current = current.BaseType;
            }
            return false;
        });
    }
}
