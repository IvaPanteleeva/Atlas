using Atlas.Api.Common;
using Atlas.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Atlas.UnitTests.Common;

public class ResultExtensionsTests
{
    [Fact]
    public void Success_WithoutData_IsNoContent()
    {
        var http = Result.Success.ToHttpResult();

        Assert.IsType<NoContent>(http);
    }

    [Fact]
    public void Success_WithData_IsOkWithData()
    {
        Result<string> result = "ok";

        var http = result.ToHttpResult();

        var ok = Assert.IsType<Ok<string>>(http);
        Assert.Equal("ok", ok.Value);
    }

    [Fact]
    public void Success_WithCustomOnSuccess_UsesIt()
    {
        Result<int> result = 7;

        var http = result.ToHttpResult(id => TypedResults.Created($"/api/sites/{id}", id));

        var created = Assert.IsType<Created<int>>(http);
        Assert.Equal("/api/sites/7", created.Location);
    }

    [Fact]
    public void NotFound_Is404ProblemDetails()
    {
        Result result = Error.NotFound("Обектът не е намерен");

        var http = result.ToHttpResult();

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("Обектът не е намерен", problem.ProblemDetails.Title);
    }

    [Fact]
    public void Conflict_Is409ProblemDetails()
    {
        Result<int> result = Error.Conflict("Вече съществува");

        var http = result.ToHttpResult();

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("Вече съществува", problem.ProblemDetails.Title);
    }

    [Fact]
    public void Validation_Is400WithErrorsPerField()
    {
        Result result = Error.Validation(new Dictionary<string, string[]>
        {
            ["name"] = ["Задължително поле"],
            ["rate"] = ["Трябва да е положително"],
        });

        var http = result.ToHttpResult();

        var problem = Assert.IsType<ValidationProblem>(http);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(["Задължително поле"], problem.ProblemDetails.Errors["name"]);
        Assert.Equal(["Трябва да е положително"], problem.ProblemDetails.Errors["rate"]);
    }
}
