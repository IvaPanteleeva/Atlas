using Atlas.Application.Users;
using Atlas.Application.Validation;

namespace Atlas.UnitTests.Users;

public class SetPasswordRequestValidatorTests
{
    [Fact]
    public void EightCharacters_IsValid()
    {
        Assert.True(new SetPasswordRequestValidator().Validate(new SetPasswordRequest("abcdefgh")).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcdefg")]
    public void EmptyOrShort_IsInvalid_OnPassword_WithBulgarianFieldName(string password)
    {
        var result = new SetPasswordRequestValidator().Validate(new SetPasswordRequest(password));

        var error = Assert.Single(result.Errors);
        Assert.Equal("Password", error.PropertyName);
        Assert.Contains("Парола", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void NullPassword_IsInvalid()
    {
        var result = new SetPasswordRequestValidator().Validate(new SetPasswordRequest(null!));

        Assert.Equal("Password", Assert.Single(result.Errors).PropertyName);
    }
}
