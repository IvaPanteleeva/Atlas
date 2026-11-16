using Atlas.Application.Identity;
using Atlas.Application.Users;
using Atlas.Application.Validation;

namespace Atlas.UnitTests.Users;

public class CreateUserRequestValidatorTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static string[] InvalidFields(CreateUserRequest request)
        => new CreateUserRequestValidator()
            .Validate(request)
            .Errors
            .Select(e => e.PropertyName)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToArray();

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Crew)]
    public void OwnerOrCrew_WithoutCompany_IsValid(string role)
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("ivan", "password", role, null)));
    }

    [Fact]
    public void ClientViewer_WithCompany_IsValid()
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("zhenistroy", "password", Roles.ClientViewer, CompanyId)));
    }

    [Fact]
    public void ClientViewer_WithoutCompany_IsInvalid()
    {
        Assert.Equal(
            ["CompanyId"],
            InvalidFields(new CreateUserRequest("zhenistroy", "password", Roles.ClientViewer, null)));
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Crew)]
    public void OwnerOrCrew_WithCompany_IsInvalid(string role)
    {
        Assert.Equal(["CompanyId"], InvalidFields(new CreateUserRequest("ivan", "password", role, CompanyId)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("owner")]
    public void UnknownRole_IsInvalid(string role)
    {
        Assert.Contains("Role", InvalidFields(new CreateUserRequest("ivan", "password", role, null)));
    }

    [Fact]
    public void Password_SevenCharacters_IsInvalid()
    {
        Assert.Equal(["Password"], InvalidFields(new CreateUserRequest("ivan", "1234567", Roles.Crew, null)));
    }

    [Fact]
    public void Password_EightCharacters_IsValid()
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("ivan", "12345678", Roles.Crew, null)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyUserName_IsInvalid(string userName)
    {
        Assert.Equal(["UserName"], InvalidFields(new CreateUserRequest(userName, "password", Roles.Crew, null)));
    }

    [Fact]
    public void UserName_LongerThan50_IsInvalid()
    {
        Assert.Equal(
            ["UserName"],
            InvalidFields(new CreateUserRequest(new string('a', 51), "password", Roles.Crew, null)));
    }

    [Theory]
    [InlineData("иван петров")]
    [InlineData("ivan/petrov")]
    public void UserName_WithSpaceOrOtherSymbol_IsInvalid_WithBulgarianMessage(string userName)
    {
        var errors = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest(userName, "password", Roles.Crew, null))
            .Errors;

        var error = Assert.Single(errors);
        Assert.Equal("UserName", error.PropertyName);
        Assert.Equal(
            "Потребителското име може да съдържа само букви (латиница или кирилица), цифри и - . _ @, без интервали.",
            error.ErrorMessage);
    }

    [Fact]
    public void CyrillicUserName_IsValid()
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("иван.петров", "password", Roles.Crew, null)));
    }
}
