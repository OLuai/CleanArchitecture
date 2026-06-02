namespace CleanArchitecture.Infrastructure.IdGeneration;

public class IdSequence
{
    public string Radical { get; set; } = default!;

    public long CurrentValue { get; set; }
}
