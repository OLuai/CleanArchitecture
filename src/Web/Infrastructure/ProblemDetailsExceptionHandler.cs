using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Exceptions;
using NotFoundException = CleanArchitecture.Application.Common.Exceptions.NotFoundException;

namespace CleanArchitecture.Web.Infrastructure;
/// <summary>
/// Converts every exception thrown by the API into an RFC 9110-compliant <see cref="ProblemDetails"/>
/// JSON response. Well-known exceptions are mapped explicitly
/// (<see cref="ValidationException"/> → 400, <see cref="NotFoundException"/> → 404,
/// <see cref="UnauthorizedAccessException"/> → 401, <see cref="ForbiddenAccessException"/> → 403,
/// <see cref="FriendlyException"/> → custom status). Any other exception falls through to a 500
/// <see cref="ProblemDetails"/> response so the client always receives a JSON body describing the error.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(
        IHostEnvironment environment,
        ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, (ProblemDetails)new ValidationProblemDetails(ve.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Une ou plusieurs erreurs de validation sont survenues.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = ve.Message
            }),
            NotFoundException ne => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "La ressource spécifiée est introuvable.",
                Detail = ne.Message
            }),
            FriendlyException fe => (fe.StatusCode, BuildFriendly(fe)),
            UnauthorizedAccessException ue => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Detail = ue.Message
            }),
            ForbiddenAccessException ffe => (StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                Detail = ffe.Message
            }),
            _ => (StatusCodes.Status500InternalServerError, BuildUnhandled(exception))
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static ProblemDetails BuildFriendly(FriendlyException fe)
    {
        var details = new ProblemDetails
        {
            Status = fe.StatusCode,
            Title = fe.Title,
            Detail = fe.Message
        };
        if (!string.IsNullOrEmpty(fe.ErrorCode))
            details.Extensions["errorCode"] = fe.ErrorCode;
        return details;
    }

    private ProblemDetails BuildUnhandled(Exception exception)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Une erreur interne est survenue.",
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : "Une erreur interne est survenue lors du traitement de la requête."
        };

        if (_environment.IsDevelopment())
        {
            details.Extensions["exceptionType"] = exception.GetType().FullName;
            details.Extensions["stackTrace"] = exception.StackTrace;
            if (exception.InnerException is not null)
                details.Extensions["innerException"] = exception.InnerException.Message;
        }

        return details;
    }
}
