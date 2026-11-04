using FluentValidation.Results;
using ValidationException = CleanArchitecture.Application.Common.Exceptions.ValidationException;

namespace CleanArchitecture.Application.Common.Models;

public static class ResultExtensions
{
    /// <summary>
    /// Turns a failed <see cref="Result"/> into a <see cref="ValidationException"/>, so the
    /// global handler renders it as a 400 with a per-field <c>errors</c> dictionary.
    /// <para>
    /// ASP.NET Identity reports failures as prose ("Username 'x' is already taken.",
    /// "Passwords must have at least one digit."), not as field names. Its messages reliably
    /// name the field they concern, so match on that to attach the error to the right input;
    /// anything unrecognised becomes a form-level message.
    /// </para>
    /// </summary>
    public static void EnsureSuccess(this Result result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationException(
            result.Errors.Select(e => new ValidationFailure(GuessProperty(e), e)));
    }

    private static string GuessProperty(string message)
    {
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase)) return "Password";
        if (message.Contains("email", StringComparison.OrdinalIgnoreCase)) return "Email";
        if (message.Contains("user name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("username", StringComparison.OrdinalIgnoreCase)) return "UserName";
        if (message.Contains("role", StringComparison.OrdinalIgnoreCase)) return "Roles";
        return string.Empty;
    }
}
