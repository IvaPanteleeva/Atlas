using Atlas.Api.Common;
using Atlas.Application;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Api.Tests.Common;

public class ValidationFilterTests
{
    public sealed record TestRequest(string Name, decimal? UnitRate);

    public sealed record RequestWithoutValidator(string Name);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.UnitRate).GreaterThan(0);
        }
    }

    private static readonly object NextResult = new();

    private static EndpointFilterDelegate Next(Action onCalled) => _ =>
    {
        onCalled();
        return ValueTask.FromResult<object?>(NextResult);
    };

    private static DefaultEndpointFilterInvocationContext Context(params object[] arguments)
    {
        var services = new ServiceCollection()
            .AddApplication(new ConfigurationBuilder().Build())
            .AddScoped<IValidator<TestRequest>, TestRequestValidator>()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        return new DefaultEndpointFilterInvocationContext(httpContext, arguments);
    }

    [Fact]
    public async Task ValidInput_CallsNext()
    {
        var called = false;

        var result = await new ValidationFilter<TestRequest>()
            .InvokeAsync(Context(new TestRequest("Баня", 25m)), Next(() => called = true));

        Assert.True(called);
        Assert.Same(NextResult, result);
    }

    [Fact]
    public async Task InvalidInput_Returns400_WithCamelCaseFields_AndBulgarianMessages()
    {
        var called = false;

        var result = await new ValidationFilter<TestRequest>()
            .InvokeAsync(Context(new TestRequest("", -1m)), Next(() => called = true));

        Assert.False(called);
        var problem = Assert.IsType<ValidationProblem>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(["name", "unitRate"], problem.ProblemDetails.Errors.Keys.Order());

        // FluentValidation's built-in BulgarianLanguage, enabled by AddApplication.
        var nameMessage = Assert.Single(problem.ProblemDetails.Errors["name"]);
        Assert.Equal("'Name' не трябва да бъде празно.", nameMessage);
    }

    [Fact]
    public async Task TypeWithoutValidator_CallsNext()
    {
        var called = false;

        var result = await new ValidationFilter<RequestWithoutValidator>()
            .InvokeAsync(Context(new RequestWithoutValidator("")), Next(() => called = true));

        Assert.True(called);
        Assert.Same(NextResult, result);
    }
}
