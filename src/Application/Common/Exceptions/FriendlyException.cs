namespace CleanArchitecture.Application.Common.Exceptions;

/// <summary>
/// Exception deliberately thrown by business code to pass an understandable message
/// on to the API client. The handler converts it into a ProblemDetails using the
/// <see cref="Title"/>, <see cref="ErrorCode"/> and <see cref="StatusCode"/> fields.
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
