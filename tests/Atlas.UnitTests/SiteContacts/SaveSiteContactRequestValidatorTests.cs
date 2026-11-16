using Atlas.Application.SiteContacts;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.SiteContacts;

public class SaveSiteContactRequestValidatorTests
{
    private static ValidationResult Validate(
        string name = "Г-н Петров",
        string? role = null,
        string? phone = null,
        string? note = null)
        => new SaveSiteContactRequestValidator().Validate(new SaveSiteContactRequest(name, role, phone, note));

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
    public void AllFieldsAtMaxLength_WithSpacesAround_IsValid()
    {
        var result = Validate(
            "  " + new string('а', 100) + "  ",
            "  " + new string('а', 50) + "  ",
            "  " + new string('1', 30) + "  ",
            new string('а', 500));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsInvalid_WithBulgarianFieldName(string name)
    {
        Assert.Contains("Име", SingleError(Validate(name: name), "Name").ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void NullName_IsInvalid()
    {
        SingleError(Validate(name: null!), "Name");
    }

    [Fact]
    public void Name_101Characters_IsInvalid()
    {
        SingleError(Validate(name: new string('а', 101)), "Name");
    }

    [Fact]
    public void Role_51Characters_IsInvalid_WithBulgarianFieldName()
    {
        Assert.Contains("Роля", SingleError(Validate(role: new string('а', 51)), "Role").ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_31Characters_IsInvalid_WithBulgarianFieldName()
    {
        Assert.Contains("Телефон", SingleError(Validate(phone: new string('1', 31)), "Phone").ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Note_501Characters_IsInvalid_WithBulgarianFieldName()
    {
        Assert.Contains("Бележка", SingleError(Validate(note: new string('а', 501)), "Note").ErrorMessage, StringComparison.Ordinal);
    }
}
