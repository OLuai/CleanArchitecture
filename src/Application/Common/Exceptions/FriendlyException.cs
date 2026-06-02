namespace CleanArchitecture.Application.Common.Exceptions;

/// <summary>
/// Exception levée volontairement par le code métier pour transmettre un message
/// compréhensible au client de l'API. Le handler convertit cette exception en
/// ProblemDetails en utilisant les champs <see cref="Title"/>, <see cref="ErrorCode"/>
/// et <see cref="StatusCode"/>.
/// </summary>
public class FriendlyException : Exception
{
    public string Title { get; }

    public string? ErrorCode { get; }

    public int StatusCode { get; }

    public FriendlyException(
        string message,
        string title = "Une erreur est survenue.",
        string? errorCode = null,
        int statusCode = 400)
        : base(message)
    {
        Title = title;
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
