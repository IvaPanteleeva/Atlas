using Atlas.Application.Common;

namespace Atlas.Api.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success;

        Assert.True(result.Succeeded);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Error_ConvertsImplicitly_ToFailedResult()
    {
        Result result = Error.NotFound("Обектът не е намерен");

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Обектът не е намерен", result.Error.Message);
    }

    [Fact]
    public void Data_ConvertsImplicitly_ToSuccessfulGenericResult()
    {
        Result<int> result = 42;

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void SuccessWith_KeepsData()
    {
        var result = Result<string>.SuccessWith("ok");

        Assert.True(result.Succeeded);
        Assert.Equal("ok", result.Data);
    }

    [Fact]
    public void Error_ConvertsImplicitly_ToFailedGenericResult()
    {
        Result<int> result = Error.Conflict("Вече съществува");

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void Data_OnFailedResult_Throws()
    {
        Result<int> result = Error.NotFound("Няма го");

        Assert.Throws<InvalidOperationException>(() => result.Data);
    }

    [Fact]
    public void ValidationError_KeepsFieldErrors()
    {
        var fieldErrors = new Dictionary<string, string[]> { ["name"] = ["Задължително поле"] };

        var error = Error.Validation(fieldErrors);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(["Задължително поле"], error.FieldErrors!["name"]);
    }
}
