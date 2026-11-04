namespace CleanArchitecture.Domain.Common;

/// <summary>
/// Entity carrying a readable business code (e.g. "CLI-000042"), distinct from its technical Id.
/// The code is generated on save when the entity type is registered with RegisterEntityCode.
/// </summary>
public interface IHasCode
{
    string Code { get; set; }
}
