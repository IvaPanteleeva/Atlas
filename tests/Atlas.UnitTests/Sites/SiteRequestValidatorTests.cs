using Atlas.Application.Sites;
using Atlas.Application.Validation;
using FluentValidation.Results;

namespace Atlas.UnitTests.Sites;

// Create and Update share the field rules, so most cases run against both validators.
// Only Status differs: optional on create (Lead), required on update.
public class SiteRequestValidatorTests
{
    public static TheoryData<string> Kinds => ["create", "update"];

    private static ValidationResult Validate(
        string kind,
        string name = "Баня",
        SiteStatus? status = SiteStatus.Lead,
        string? endCustomer = null,
        string? address = null,
        string? mapUrl = null,
        string? note = null)
        => kind == "create"
            ? new CreateSiteRequestValidator().Validate(
                new CreateSiteRequest(name, status, null, endCustomer, address, mapUrl, note))
            : new UpdateSiteRequestValidator().Validate(
                new UpdateSiteRequest(name, status, null, endCustomer, address, mapUrl, note));

    private static ValidationFailure SingleError(ValidationResult result, string property)
    {
        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
        return error;
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MinimalRequest_IsValid(string kind)
    {
        Assert.True(Validate(kind).IsValid);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void FullRequest_AtTheLimits_IsValid(string kind)
    {
        var result = Validate(
            kind,
            name: new string('а', SiteRules.NameMaxLength),
            status: SiteStatus.Done,
            endCustomer: new string('б', SiteRules.EndCustomerMaxLength),
            address: new string('в', SiteRules.AddressMaxLength),
            mapUrl: "https://maps.google.com/?q=" + new string('1', SiteRules.MapUrlMaxLength - 27),
            note: new string('г', SiteRules.NoteMaxLength));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
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
        SingleError(Validate(kind, name: new string('а', SiteRules.NameMaxLength + 1)), "Name");
    }

    // Limits are for the stored (trimmed) values.
    [Theory]
    [MemberData(nameof(Kinds))]
    public void Limits_AreCheckedAfterTrim(string kind)
    {
        var result = Validate(
            kind,
            name: "  " + new string('а', SiteRules.NameMaxLength) + "  ",
            endCustomer: " " + new string('б', SiteRules.EndCustomerMaxLength) + " ",
            address: " " + new string('в', SiteRules.AddressMaxLength) + " ",
            note: " " + new string('г', SiteRules.NoteMaxLength) + " ");

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EndCustomer_TooLong_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(
            Validate(kind, endCustomer: new string('б', SiteRules.EndCustomerMaxLength + 1)),
            "EndCustomer");
        Assert.Contains("Краен клиент", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Address_TooLong_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(Validate(kind, address: new string('в', SiteRules.AddressMaxLength + 1)), "Address");
        Assert.Contains("Адрес", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Note_TooLong_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(Validate(kind, note: new string('г', SiteRules.NoteMaxLength + 1)), "Note");
        Assert.Contains("Бележка", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MapUrl_TooLong_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var url = "https://maps.google.com/?q=" + new string('1', SiteRules.MapUrlMaxLength);
        var error = SingleError(Validate(kind, mapUrl: url), "MapUrl");
        Assert.Contains("Линк към карта", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("create", "maps.google.com")]
    [InlineData("create", "ftp://x")]
    [InlineData("update", "maps.google.com")]
    [InlineData("update", "ftp://x")]
    [InlineData("update", "javascript:alert(1)")]
    public void MapUrl_NotHttp_IsInvalid_WithBulgarianMessage(string kind, string mapUrl)
    {
        var error = SingleError(Validate(kind, mapUrl: mapUrl), "MapUrl");
        Assert.Equal("Линкът трябва да започва с http:// или https://", error.ErrorMessage);
    }

    // Empty optional text becomes null (no value), so it is not a bad link.
    [Theory]
    [InlineData("create", "")]
    [InlineData("create", "   ")]
    [InlineData("update", "")]
    [InlineData("update", " ")]
    public void EmptyOptionalFields_AreValid(string kind, string empty)
    {
        Assert.True(Validate(kind, endCustomer: empty, address: empty, mapUrl: empty, note: empty).IsValid);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Status_NotDefined_IsInvalid_WithBulgarianFieldName(string kind)
    {
        var error = SingleError(Validate(kind, status: (SiteStatus)7), "Status");
        Assert.Contains("Статус", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithoutStatus_IsValid()
    {
        Assert.True(Validate("create", status: null).IsValid);
    }

    [Fact]
    public void Update_WithoutStatus_IsInvalid()
    {
        var error = SingleError(Validate("update", status: null), "Status");
        Assert.Contains("Статус", error.ErrorMessage, StringComparison.Ordinal);
    }
}
