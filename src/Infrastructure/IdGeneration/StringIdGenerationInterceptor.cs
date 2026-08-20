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

            var idProperty = entry.Property(nameof(BaseEntity<string>.Id));

            // TemporaryStringIdValueGenerator put a placeholder here so EF could track the entity.
            // It is a non-empty string, so the temporary flag — not emptiness — is what separates
            // "EF filled this in" from "the caller chose this id", which is left untouched.
            var needsId = idProperty.IsTemporary
                || idProperty.CurrentValue is not string current
                || string.IsNullOrEmpty(current);

            if (!needsId) continue;

            if (!_registry.TryGet(type, out var registration))
            {
                throw new InvalidOperationException(
                    $"Entity '{type.FullName}' has a generated string key but no radical is registered. " +
                    $"Call services.RegisterStringId<{type.Name}>(\"...\") in Infrastructure's DependencyInjection.");
            }

            var newId = await _generator.NextAsync(registration, cancellationToken).ConfigureAwait(false);
            idProperty.CurrentValue = newId;

            // Leaving the flag set would make EF treat the column as store-generated: it would
            // omit it from the INSERT and wait for a value the database never sends back.
            idProperty.IsTemporary = false;
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
