using System.Text.Json;
using FluentValidation.Results;

namespace CleanArchitecture.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => ToCamelCase(e.PropertyName), e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return propertyName;

        // FluentValidation may emit nested paths like "Address.PostalCode" — convert each segment.
        var segments = propertyName.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            if (!string.IsNullOrEmpty(segments[i]))
                segments[i] = JsonNamingPolicy.CamelCase.ConvertName(segments[i]);
        }
        return string.Join('.', segments);
    }
}
