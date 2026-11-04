namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>How an entity's business code is shaped: <c>PRJ</c> + <c>-</c> + <c>000123</c>.</summary>
public sealed record CodeRegistration(
    Type EntityType,
    string Radical,
    int PadLength = 6,
    string Separator = "-",
    int HiLoBlockSize = 50);
