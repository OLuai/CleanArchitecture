namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed record StringIdRegistration(
    Type EntityType,
    string Radical,
    int PadLength = 6,
    string Separator = "-",
    int HiLoBlockSize = 50);
