using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>
/// Primary keys as UUID version 7 rather than the random version 4.
/// <para>
/// A v7 embeds a millisecond timestamp in its high bits, so successive keys sort close together
/// and inserts land at the right-hand edge of the primary key's B-tree instead of scattering
/// across it. With random keys every insert dirties a different page, which costs write
/// amplification and cache locality on a table that only ever grows.
/// </para>
/// <para>
/// Generated in the application rather than by the database, so an entity has its identity from
/// the moment it is constructed: no round-trip, and no <c>RETURNING</c> to read it back.
/// </para>
/// </summary>
public sealed class SequentialGuidValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}
