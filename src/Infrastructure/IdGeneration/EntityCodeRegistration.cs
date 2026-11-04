namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed record EntityCodeRegistration(
    Type EntityType,
    string Radical,
    int PadLength = 6,
    string Separator = "-",
    int HiLoBlockSize = 50);
