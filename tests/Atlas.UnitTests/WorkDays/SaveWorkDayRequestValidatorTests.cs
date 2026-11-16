using System.Globalization;
using Atlas.Application.Common;
using Atlas.Application.Validation;
using Atlas.Application.WorkDays;
using Atlas.UnitTests.Fakes;
using FluentValidation.Results;

namespace Atlas.UnitTests.WorkDays;

public class SaveWorkDayRequestValidatorTests
{
    // 23:30 in Sofia on 16.11.2026 (UTC+2): still the 16th here, the 16th in UTC too.
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 11, 16, 21, 30, 0, TimeSpan.Zero));

    private static readonly DateOnly Today = new(2026, 11, 16);

    private static SaveWorkItemRequest Item(
        string? description = "Шпакловка",
        string quantity = "2",
        Unit? unit = Unit.SquareMeter,
        decimal? rate = 6.5m)
        => new(null, null, description, decimal.Parse(quantity, CultureInfo.InvariantCulture), unit, rate, false);

    private static SaveWorkDayRequest Day(
        DateOnly? date = null,
        string? title = null,
        string? note = null,
        IReadOnlyList<SaveWorkItemRequest>? items = null)
        => new(date ?? Today, title, note, items ?? [Item()]);

    private static ValidationResult Validate(SaveWorkDayRequest request)
        => new SaveWorkDayRequestValidator(Clock).Validate(request);

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Fact]
    public void MinimalRequest_IsValid()
    {
        var result = Validate(Day());
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void FullRequest_WithPastDate_IsValid()
    {
        var result = Validate(Day(
            Today.AddDays(-30),
            new string('т', 200),
            new string('б', 2000),
            [Item(rate: null), Item("Боя", "1.333", Unit.LinearMeter, 100000m)]));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // The clock is 00:30 on the 17th in Sofia, but still the 16th in UTC: the 17th is allowed.
    [Fact]
    public void Today_IsTheSofiaDate_NotTheUtcDate()
    {
        var validator = new SaveWorkDayRequestValidator(
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 16, 22, 30, 0, TimeSpan.Zero)));

        var result = validator.Validate(Day(new DateOnly(2026, 11, 17)));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void FutureDate_IsInvalid()
    {
        var error = SingleError(Validate(Day(Today.AddDays(1))), "Date");
        Assert.Equal(WorkDayRules.FutureDateMessage, error.ErrorMessage);
    }

    [Fact]
    public void MissingDate_IsInvalid()
    {
        var error = SingleError(Validate(new SaveWorkDayRequest(null, null, null, [Item()])), "Date");
        Assert.Contains("Дата", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void NoItems_IsInvalid()
    {
        Assert.Equal(WorkDayRules.NoItemsMessage, SingleError(Validate(Day(items: [])), "Items").ErrorMessage);
        Assert.Equal(
            WorkDayRules.NoItemsMessage,
            SingleError(Validate(new SaveWorkDayRequest(Today, null, null, null)), "Items").ErrorMessage);
    }

    [Fact]
    public void FiftyItems_IsValid_FiftyOne_IsInvalid()
    {
        Assert.True(Validate(Day(items: [.. Enumerable.Range(0, 50).Select(_ => Item())])).IsValid);

        var error = SingleError(Validate(Day(items: [.. Enumerable.Range(0, 51).Select(_ => Item())])), "Items");
        Assert.Equal(WorkDayRules.TooManyItemsMessage, error.ErrorMessage);
    }

    [Fact]
    public void NullItem_IsInvalid()
    {
        SingleError(Validate(Day(items: [Item(), null!])), "Items[1]");
    }

    [Theory]
    [InlineData("0", WorkDayRules.QuantityMustBePositiveMessage)]
    [InlineData("-1", WorkDayRules.QuantityMustBePositiveMessage)]
    [InlineData("100000.001", WorkDayRules.QuantityTooLargeMessage)]
    [InlineData("1.2345", WorkDayRules.QuantityTooManyDecimalsMessage)]
    public void Quantity_Bad_IsInvalid_OnTheItem(string quantity, string message)
    {
        var error = SingleError(Validate(Day(items: [Item(), Item(quantity: quantity)])), "Items[1].Quantity");
        Assert.Equal(message, error.ErrorMessage);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("100000")]
    public void Quantity_Edges_AreValid(string quantity)
    {
        Assert.True(Validate(Day(items: [Item(quantity: quantity)])).IsValid);
    }

    [Theory]
    [InlineData("0", MoneyRules.MustBePositiveMessage)]
    [InlineData("100000.01", MoneyRules.TooLargeMessage)]
    [InlineData("6.555", MoneyRules.TooManyDecimalsMessage)]
    public void Rate_Bad_IsInvalid_OnTheItem(string rate, string message)
    {
        var error = SingleError(
            Validate(Day(items: [Item(rate: decimal.Parse(rate, CultureInfo.InvariantCulture))])),
            "Items[0].Rate");
        Assert.Equal(message, error.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_Missing_IsInvalid(string? description)
    {
        SingleError(Validate(Day(items: [Item(description: description)])), "Items[0].Description");
    }

    [Fact]
    public void Description_IsCheckedTrimmed()
    {
        Assert.True(Validate(Day(items: [Item(description: "  " + new string('ш', 200) + "  ")])).IsValid);
        SingleError(Validate(Day(items: [Item(description: new string('ш', 201))])), "Items[0].Description");
    }

    [Fact]
    public void MissingUnit_IsInvalid()
    {
        var error = SingleError(Validate(Day(items: [Item(unit: null)])), "Items[0].Unit");
        Assert.Contains("Мярка", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownUnit_IsInvalid()
    {
        SingleError(Validate(Day(items: [Item(unit: (Unit)42)])), "Items[0].Unit");
    }

    [Fact]
    public void Title_And_Note_TooLong_AreInvalid()
    {
        SingleError(Validate(Day(title: new string('т', 201))), "Title");
        SingleError(Validate(Day(note: new string('б', 2001))), "Note");
    }

    [Fact]
    public void Title_And_Note_AreCheckedTrimmed()
    {
        var result = Validate(Day(title: " " + new string('т', 200) + " ", note: "   "));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }
}
