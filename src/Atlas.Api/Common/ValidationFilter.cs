namespace Atlas.Api.Common;

using System.Text.Json;
using Atlas.Application.Common;
using FluentValidation;

// Runs the FluentValidation validator for the endpoint argument of type T.
// Validators are scoped, so they come from the request's services, not the constructor.
public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (validator is null || argument is null)
        {
            return await next(context);
        }

        var validation = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (validation.IsValid)
        {
            return await next(context);
        }

        var fieldErrors = validation.Errors
            .GroupBy(failure => ToJsonPath(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return Error.Validation(fieldErrors).ToProblem();
    }

    // "Items[0].UnitRate" → "items[0].unitRate", matching the JSON property names.
    private static string ToJsonPath(string propertyName)
        => string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        => builder
            .AddEndpointFilter<ValidationFilter<T>>()
            .ProducesValidationProblem();
}
