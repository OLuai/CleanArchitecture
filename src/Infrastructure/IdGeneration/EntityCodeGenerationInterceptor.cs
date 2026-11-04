using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Infrastructure.IdGeneration;

/// <summary>
/// Assigns a HiLo code (radical + padded number) to added entities implementing <see cref="IHasCode"/>
/// whose type is registered with RegisterEntityCode and whose Code is still empty.
/// </summary>
public sealed class EntityCodeGenerationInterceptor : SaveChangesInterceptor
{
    private readonly EntityCodeRegistry _registry;
    private readonly EntityCodeGenerator _generator;

    public EntityCodeGenerationInterceptor(EntityCodeRegistry registry, EntityCodeGenerator generator)
    {
        _registry = registry;
        _generator = generator;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AssignCodesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await AssignCodesAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task AssignCodesAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IHasCode>())
        {
            if (entry.State != EntityState.Added) continue;
            if (!string.IsNullOrEmpty(entry.Entity.Code)) continue;
            if (!_registry.TryGet(entry.Entity.GetType(), out var registration)) continue;

            entry.Entity.Code = await _generator.NextAsync(registration, cancellationToken).ConfigureAwait(false);
        }
    }
}
