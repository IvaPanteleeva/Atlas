using Atlas.Application.Companies;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.Companies;

// Create and Update have the same rule for Name, so every case runs against both validators.
public class CompanyRequestValidatorTests
{
    public static TheoryData<string> Kinds => ["create", "update"];

    private static ValidationResult Validate(string kind, string name)
        => kind == "create"
            ? new CreateCompanyRequestValidator().Validate(new CreateCompanyRequest(name))
            : new UpdateCompanyRequestValidator().Validate(new UpdateCompanyRequest(name));

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_IsValid(string kind)
    {
        Assert.True(Validate(kind, "ЖЕНИСТРОЙ ЕООД").IsValid);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_200Characters_IsValid(string kind)
    {
        Assert.True(Validate(kind, new string('а', CompanyRules.NameMaxLength)).IsValid);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_201Characters_IsInvalid(string kind)
    {
        var error = Assert.Single(Validate(kind, new string('а', CompanyRules.NameMaxLength + 1)).Errors);
        Assert.Equal("Name", error.PropertyName);
    }

    // The limit is for the stored (trimmed) name.
    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_200CharactersWithSurroundingSpaces_IsValid(string kind)
    {
        Assert.True(Validate(kind, "  " + new string('а', CompanyRules.NameMaxLength) + "  ").IsValid);
    }

    [Theory]
    [InlineData("create", "")]
    [InlineData("create", "   ")]
    [InlineData("update", "")]
    [InlineData("update", "   ")]
    public void EmptyName_IsInvalid_WithBulgarianFieldName(string kind, string name)
    {
        var error = Assert.Single(Validate(kind, name).Errors);
        Assert.Equal("Name", error.PropertyName);
        Assert.Contains("Име", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void NullName_IsInvalid(string kind)
    {
        var error = Assert.Single(Validate(kind, null!).Errors);
        Assert.Equal("Name", error.PropertyName);
    }
}
