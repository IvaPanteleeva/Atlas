using Atlas.Application.Tasks;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.Tasks;

public class TaskRequestValidatorTests
{
    private static ValidationResult ValidateCreate(string text, Guid? siteId = null, Guid? assigneeId = null)
        => new CreateTaskRequestValidator().Validate(new CreateTaskRequest(text, siteId, assigneeId));

    private static ValidationResult ValidateUpdate(string text, Guid? assigneeId = null)
        => new UpdateTaskRequestValidator().Validate(new UpdateTaskRequest(text, assigneeId));

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Fact]
    public void Create_TextOnly_IsValid()
    {
        Assert.True(ValidateCreate("Купи лепило").IsValid);
    }

    [Fact]
    public void Create_TextAtMaxLength_WithSpacesAround_SiteAndAssignee_IsValid()
    {
        var result = ValidateCreate("  " + new string('а', 500) + "  ", Guid.NewGuid(), Guid.NewGuid());
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyText_IsInvalid_WithBulgarianFieldName(string text)
    {
        Assert.Contains("Текст", SingleError(ValidateCreate(text), "Text").ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_NullText_IsInvalid()
    {
        SingleError(ValidateCreate(null!), "Text");
    }

    [Fact]
    public void Create_Text501Characters_IsInvalid()
    {
        SingleError(ValidateCreate(new string('а', 501)), "Text");
    }

    [Fact]
    public void Update_TextAndAssignee_IsValid()
    {
        Assert.True(ValidateUpdate(" Обади се на Петров ", Guid.NewGuid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyText_IsInvalid(string text)
    {
        SingleError(ValidateUpdate(text), "Text");
    }

    [Fact]
    public void Update_Text501Characters_IsInvalid()
    {
        SingleError(ValidateUpdate(new string('а', 501)), "Text");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetDone_WithValue_IsValid(bool done)
    {
        Assert.True(new SetTaskDoneRequestValidator().Validate(new SetTaskDoneRequest(done)).IsValid);
    }

    [Fact]
    public void SetDone_Missing_IsInvalid()
    {
        SingleError(new SetTaskDoneRequestValidator().Validate(new SetTaskDoneRequest(null)), "Done");
    }
}
