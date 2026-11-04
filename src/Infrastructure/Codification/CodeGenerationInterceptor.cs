using CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>
/// Fills in the business code of any <see cref="IHasCode"/> entity being inserted.
/// <para>
/// It runs at SaveChanges rather than at Add because reserving a HiLo block is a database
/// round-trip. That was impossible while the code doubled as the primary key (EF cannot track an
/// entity whose key is still null) but with a <see cref="Guid"/> key the entity is trackable from
/// the moment it is added, and the code can be assigned at the last responsible moment.
/// </para>
/// </summary>
public sealed class CodeGenerationInterceptor : SaveChangesInterceptor
{
    private readonly CodeRegistry _registry;
    private readonly HiLoCodeGenerator _generator;

    public CodeGenerationInterceptor(CodeRegistry registry, HiLoCodeGenerator generator)
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

            // A caller-supplied code wins: importing existing references must stay possible.
            if (!string.IsNullOrEmpty(entry.Entity.Code)) continue;

            if (!_registry.TryGet(entry.Entity.GetType(), out var registration)) continue;

            entry.Entity.Code = await _generator
                .NextAsync(registration, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
