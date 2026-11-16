namespace Atlas.Application.Common;

/// <summary>Kind of failure; the API maps it to an HTTP status code.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

/// <summary>A failure carried by <see cref="Result"/>.</summary>
/// <param name="Type">Kind of failure.</param>
/// <param name="Message">Human-readable message (Bulgarian, shown to the user).</param>
/// <param name="FieldErrors">Messages per field (camelCase, as in JSON); only for <see cref="ErrorType.Validation"/>.</param>
public sealed record Error(
    ErrorType Type,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public const string ValidationMessage = "Невалидни данни";

    public static Error NotFound(string message)
        => new(ErrorType.NotFound, message);

    public static Error Conflict(string message)
        => new(ErrorType.Conflict, message);

    // Failed sign-in (wrong credentials or locked out).
    public static Error Unauthorized(string message)
        => new(ErrorType.Unauthorized, message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors)
        => new(ErrorType.Validation, ValidationMessage, fieldErrors);
}
