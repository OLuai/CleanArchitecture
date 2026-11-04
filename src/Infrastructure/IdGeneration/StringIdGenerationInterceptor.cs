using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Infrastructure.IdGeneration;

/// <summary>
/// Assigns a GUID to string-keyed entities (BaseEntity&lt;string&gt;) that are added without an Id,
/// in the same format as ASP.NET Core Identity user Ids.
/// </summary>
public sealed class StringIdGenerationInterceptor : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, bool> s_isStringEntityCache = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AssignIds(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignIds(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AssignIds(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;
            if (!IsStringKeyedBaseEntity(entry.Entity.GetType())) continue;

            var idProperty = entry.Property(nameof(BaseEntity<string>.Id));
            if (idProperty.CurrentValue is string current && !string.IsNullOrEmpty(current)) continue;

            idProperty.CurrentValue = Guid.NewGuid().ToString();
        }
    }

    private static bool IsStringKeyedBaseEntity(Type type)
    {
        return s_isStringEntityCache.GetOrAdd(type, static t =>
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
