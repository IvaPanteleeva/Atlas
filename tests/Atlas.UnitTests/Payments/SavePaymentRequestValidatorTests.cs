using System.Globalization;
using Atlas.Application.Payments;
using Atlas.Application.Validation;
using Atlas.Application.WorkDays;
using Atlas.UnitTests.Fakes;
using FluentValidation.Results;

namespace Atlas.UnitTests.Payments;

public class SavePaymentRequestValidatorTests
{
    // 23:30 in Sofia on 16.11.2026 (UTC+2).
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 11, 16, 21, 30, 0, TimeSpan.Zero));

    private static readonly DateOnly Today = new(2026, 11, 16);

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static ValidationResult Validate(SavePaymentRequest request)
        => new SavePaymentRequestValidator(Clock).Validate(request);

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("50")]
    [InlineData("1157.89")]
    [InlineData("1000000")]
    public void ValidAmount_IsValid(string amount)
    {
        var result = Validate(new SavePaymentRequest(Today, D(amount), null));
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("0", PaymentRules.MustBePositiveMessage)]
    [InlineData("-5", PaymentRules.MustBePositiveMessage)]
    [InlineData("1000000.01", PaymentRules.TooLargeMessage)]
    [InlineData("10.005", PaymentRules.TooManyDecimalsMessage)]
    public void BadAmount_IsInvalid_WithOneMessage(string amount, string message)
    {
        var error = SingleError(Validate(new SavePaymentRequest(Today, D(amount), null)), "Amount");
        Assert.Equal(message, error.ErrorMessage);
    }

    [Fact]
    public void Messages_AreAboutTheSum()
    {
        Assert.Equal("Сумата трябва да е по-голяма от 0", PaymentRules.MustBePositiveMessage);
        Assert.Equal("Сумата може да е най-много 1 000 000 €", PaymentRules.TooLargeMessage);
        Assert.Equal("Сумата може да има най-много 2 знака след запетаята", PaymentRules.TooManyDecimalsMessage);
    }

    [Fact]
    public void MissingAmount_IsInvalid()
    {
        SingleError(Validate(new SavePaymentRequest(Today, null, null)), "Amount");
    }

    [Fact]
    public void MissingDate_IsInvalid()
    {
        SingleError(Validate(new SavePaymentRequest(null, 10m, null)), "Date");
    }

    [Fact]
    public void FutureDate_IsInvalid_PastAndToday_AreValid()
    {
        var error = SingleError(Validate(new SavePaymentRequest(Today.AddDays(1), 10m, null)), "Date");
        Assert.Equal(WorkDayRules.FutureDateMessage, error.ErrorMessage);

        Assert.True(Validate(new SavePaymentRequest(Today, 10m, null)).IsValid);
        Assert.True(Validate(new SavePaymentRequest(Today.AddYears(-1), 10m, null)).IsValid);
    }

    // 00:30 on the 17th in Sofia is still the 16th in UTC: the 17th is "today".
    [Fact]
    public void Today_IsTheSofiaDate()
    {
        var validator = new SavePaymentRequestValidator(
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 16, 22, 30, 0, TimeSpan.Zero)));

        Assert.True(validator.Validate(new SavePaymentRequest(new DateOnly(2026, 11, 17), 10m, null)).IsValid);
    }

    [Fact]
    public void Note_UpTo200_IsValid_201_IsInvalid()
    {
        Assert.True(Validate(new SavePaymentRequest(Today, 10m, new string('а', 200))).IsValid);
        SingleError(Validate(new SavePaymentRequest(Today, 10m, new string('а', 201))), "Note");
    }

    // Checked after trimming, like it is stored.
    [Fact]
    public void Note_IsCheckedTrimmed()
    {
        Assert.True(Validate(new SavePaymentRequest(Today, 10m, "  " + new string('а', 200) + "  ")).IsValid);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  аванс, в брой  ", "аванс, в брой")]
    public void NormalizeNote_TrimsAndEmptyIsNull(string? note, string? expected)
    {
        Assert.Equal(expected, PaymentRules.NormalizeNote(note));
    }
}
