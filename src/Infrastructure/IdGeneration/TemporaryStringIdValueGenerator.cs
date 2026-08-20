using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace CleanArchitecture.Infrastructure.IdGeneration;

/// <summary>
/// Hands EF Core a placeholder key so a string-keyed entity can be tracked between
/// <c>Add()</c> and <c>SaveChanges()</c>, at which point
/// <see cref="StringIdGenerationInterceptor"/> replaces it with the real HiLo id.
/// <para>
/// A string key has no value generator by default, so <c>Add()</c> throws on the null id long
/// before any <c>SaveChanges</c> interceptor runs. EF's own string generator is not usable here
/// either: it produces a permanent 36-character GUID, which the interceptor would read as a
/// caller-supplied id and which overflows the id columns this template sizes at 32 characters.
/// </para>
/// <para>
/// <see cref="GeneratesTemporaryValues"/> is what makes the difference. It marks the value as
/// temporary, so the interceptor can tell "EF filled this in" from "the caller chose this id",
/// and EF propagates the final value to any dependent foreign key once the interceptor writes it.
/// </para>
/// </summary>
public sealed class TemporaryStringIdValueGenerator : ValueGenerator<string>
{
    public override bool GeneratesTemporaryValues => true;

    /// <summary>
    /// Only has to be unique among the entities currently tracked — the value never reaches the
    /// database.
    /// </summary>
    public override string Next(EntityEntry entry) => Guid.NewGuid().ToString("N");
}
