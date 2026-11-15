namespace Atlas.Api.Common;

using Atlas.Application.Common;

public static class ResultExtensions
{
    // Success without data → 204.
    public static IResult ToHttpResult(this Result result)
        => result.Succeeded
            ? TypedResults.NoContent()
            : result.Error.ToProblem();

    // Success → 200 with data, unless onSuccess says otherwise (e.g. TypedResults.Created for POST).
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
        => result.Succeeded
            ? onSuccess?.Invoke(result.Data) ?? TypedResults.Ok(result.Data)
            : result.Error.ToProblem();

    // Every error is ProblemDetails; the status code comes from ErrorType.
    public static IResult ToProblem(this Error error)
        => error.Type switch
        {
            ErrorType.Validation => TypedResults.ValidationProblem(
                error.FieldErrors ?? new Dictionary<string, string[]>(),
                title: error.Message),
            ErrorType.NotFound => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: error.Message),
            ErrorType.Conflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: error.Message),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type."),
        };
}
