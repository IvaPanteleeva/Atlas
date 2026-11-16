using Atlas.Application.Common;
using Atlas.Application.Services;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.Services;

// Create and Update share all rules, so every case runs against both validators.
public class ServiceRequestValidatorTests
{
    public static TheoryData<string> Kinds => ["create", "update"];

    private static ValidationResult Validate(
        string kind,
        string name = "Шпакловка",
        Unit? unit = Unit.SquareMeter,
        decimal? defaultRate = null)
        => kind == "create"
            ? new CreateServiceRequestValidator().Validate(new CreateServiceRequest(name, unit, defaultRate))
            : new UpdateServiceRequestValidator().Validate(new UpdateServiceRequest(name, unit, defaultRate));

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void WithoutDefaultRate_IsValid(string kind)
    {
        Assert.True(Validate(kind).IsValid);
    }

    [Theory]
    [InlineData("create", "6.5")]
    [InlineData("create", "6.55")]
    [InlineData("create", "0.01")]
    [InlineData("update", "100000")]
    [InlineData("update", "6.50")]
    public void DefaultRate_InRange_IsValid(string kind, string rate)
    {
        var result = Validate(kind, defaultRate: decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("create", "0", MoneyRules.MustBePositiveMessage)]
    [InlineData("update", "-1", MoneyRules.MustBePositiveMessage)]
    [InlineData("create", "100000.01", MoneyRules.TooLargeMessage)]
    [InlineData("update", "6.555", MoneyRules.TooManyDecimalsMessage)]
    public void DefaultRate_Bad_IsInvalid_WithBulgarianMessage(string kind, string rate, string message)
    {
        var error = SingleError(
            Validate(kind, defaultRate: decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)),
            "DefaultRate");
        Assert.Equal(message, error.ErrorMessage);
    }

    // One message per bad price, not three.
    [Fact]
    public void DefaultRate_NegativeWithThreeDecimals_GivesOneError()
    {
        SingleError(Validate("create", defaultRate: -6.555m), "DefaultRate");
    }

    [Theory]
    [InlineData("create", "")]
    [InlineData("create", "   ")]
    [InlineData("update", "")]
    [InlineData("update", "   ")]
    public void EmptyName_IsInvalid_WithBulgarianFieldName(string kind, string name)
    {
        var error = SingleError(Validate(kind, name: name), "Name");
        Assert.Contains("Име", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void NullName_IsInvalid(string kind)
    {
        SingleError(Validate(kind, name: null!), "Name");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_201Characters_IsInvalid(string kind)
    {
        SingleError(Validate(kind, name: new string('а', ServiceRules.NameMaxLength + 1)), "Name");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Name_200CharactersWithSpacesAround_IsValid(string kind)
    {
        Assert.True(Validate(kind, name: "  " + new string('а', ServiceRules.NameMaxLength) + "  ").IsValid);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MissingUnit_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(Validate(kind, unit: null), "Unit");
        Assert.Contains("Мярка", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UndefinedUnit_IsInvalid(string kind)
    {
        SingleError(Validate(kind, unit: (Unit)9), "Unit");
    }
}
