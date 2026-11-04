namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>
/// Persistent counter per prefix, from which <c>HiLoCodeGenerator</c> reserves its blocks. One row
/// per code family (<c>PRJ</c>, <c>INV</c>, <c>PRD</c>…).
/// </summary>
public class CodeSequence
{
    public string Radical { get; set; } = default!;

    public long CurrentValue { get; set; }
}
