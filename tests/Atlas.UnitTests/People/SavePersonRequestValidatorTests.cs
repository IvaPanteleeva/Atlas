using Atlas.Application.People;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.People;

public class SavePersonRequestValidatorTests
{
    private static ValidationResult Validate(
        string name = "Иван",
        string? phone = null,
        string? note = null,
        bool isCrew = true)
        => new SavePersonRequestValidator().Validate(new SavePersonRequest(name, phone, note, isCrew));

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Fact]
    public void NameOnly_IsValid()
    {
        Assert.True(Validate().IsValid);
    }

    [Fact]
    public void AllFields_IsValid()
    {
        var result = Validate("Иван Петров", "0888 123 456", "Плочки и фуги", isCrew: false);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsInvalid_WithBulgarianFieldName(string name)
    {
        var error = SingleError(Validate(name: name), "Name");
        Assert.Contains("Име", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void NullName_IsInvalid()
    {
        SingleError(Validate(name: null!), "Name");
    }

    [Fact]
    public void Name_101Characters_IsInvalid()
    {
        SingleError(Validate(name: new string('а', PersonRules.NameMaxLength + 1)), "Name");
    }

    [Fact]
    public void Name_100CharactersWithSpacesAround_IsValid()
    {
        Assert.True(Validate(name: "  " + new string('а', PersonRules.NameMaxLength) + "  ").IsValid);
    }

    [Fact]
    public void Phone_31Characters_IsInvalid_WithBulgarianFieldName()
    {
        var error = SingleError(Validate(phone: new string('1', PersonRules.PhoneMaxLength + 1)), "Phone");
        Assert.Contains("Телефон", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_30CharactersWithSpacesAround_IsValid()
    {
        Assert.True(Validate(phone: "  " + new string('1', PersonRules.PhoneMaxLength) + "  ").IsValid);
    }

    [Fact]
    public void Note_501Characters_IsInvalid_WithBulgarianFieldName()
    {
        var error = SingleError(Validate(note: new string('а', PersonRules.NoteMaxLength + 1)), "Note");
        Assert.Contains("Бележка", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Note_500Characters_IsValid()
    {
        Assert.True(Validate(note: new string('а', PersonRules.NoteMaxLength)).IsValid);
    }

    [Fact]
    public void OnlySpaces_InPhoneAndNote_IsValid()
    {
        Assert.True(Validate(phone: "   ", note: "   ").IsValid);
    }
}
