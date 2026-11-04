namespace CleanArchitecture.Domain.Common;

/// <summary>
/// An entity carrying a readable business code, distinct from its primary key.
/// <para>
/// The key is a <see cref="Guid"/>: it only matters to the database and to relations, it is
/// generated without a round trip and it leaks nothing about the volume of data. The code
/// (<c>PRJ-000123</c>) is what a human reads, dictates over the phone and finds in a ticket;
/// it is assigned sequentially by <c>HiLoCodeGenerator</c> on insert.
/// </para>
/// <para>
/// Separating the two avoids having to choose: a stable technical key on one side, a readable
/// and unique business reference on the other.
/// </para>
/// </summary>
public interface IHasCode
{
    string Code { get; set; }
}
