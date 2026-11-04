namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>
/// Compteur persistant par radical, d'où <c>HiLoCodeGenerator</c> réserve ses blocs. Une ligne
/// par famille de codes (<c>PRJ</c>, <c>INV</c>, <c>PRD</c>…).
/// </summary>
public class CodeSequence
{
    public string Radical { get; set; } = default!;

    public long CurrentValue { get; set; }
}
