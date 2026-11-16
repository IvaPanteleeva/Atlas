using Atlas.Application.Common;
using Atlas.Application.SitePrices;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.SitePrices;

// Create and Update share the Rate rule, so the Rate cases run against both validators.
public class SitePriceRequestValidatorTests
{
    public static TheoryData<string> Kinds => ["create", "update"];

    private static ValidationResult Validate(string kind, decimal? rate)
        => kind == "create"
            ? new CreateSitePriceRequestValidator().Validate(new CreateSitePriceRequest(Guid.NewGuid(), rate))
            : new UpdateSitePriceRequestValidator().Validate(new UpdateSitePriceRequest(rate));

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Theory]
    [InlineData("create", "6.5")]
    [InlineData("create", "6.55")]
    [InlineData("update", "0.01")]
    [InlineData("update", "100000")]
    public void Rate_InRange_IsValid(string kind, string rate)
    {
        var result = Validate(kind, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MissingRate_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(Validate(kind, null), "Rate");
        Assert.Contains("Цена", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("create", "0", MoneyRules.MustBePositiveMessage)]
    [InlineData("update", "-5", MoneyRules.MustBePositiveMessage)]
    [InlineData("create", "100001", MoneyRules.TooLargeMessage)]
    [InlineData("update", "6.555", MoneyRules.TooManyDecimalsMessage)]
    public void Rate_Bad_IsInvalid_WithBulgarianMessage(string kind, string rate, string message)
    {
        var error = SingleError(Validate(kind, decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)), "Rate");
        Assert.Equal(message, error.ErrorMessage);
    }

    [Fact]
    public void Create_EmptyServiceId_IsInvalid()
    {
        var result = new CreateSitePriceRequestValidator().Validate(new CreateSitePriceRequest(Guid.Empty, 6.5m));
        SingleError(result, "ServiceId");
    }
}
