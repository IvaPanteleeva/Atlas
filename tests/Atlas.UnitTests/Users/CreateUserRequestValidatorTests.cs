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
    [InlineData(Roles.Manager)]
    public void OwnerOrManager_WithoutCompany_IsValid(string role)
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
    [InlineData(Roles.Manager)]
    public void OwnerOrManager_WithCompany_IsInvalid(string role)
    {
        Assert.Equal(["CompanyId"], InvalidFields(new CreateUserRequest("ivan", "password", role, CompanyId)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("owner")]
    [InlineData("Crew")]
    public void UnknownRole_IsInvalid(string role)
    {
        Assert.Contains("Role", InvalidFields(new CreateUserRequest("ivan", "password", role, null)));
    }

    [Fact]
    public void Password_SevenCharacters_IsInvalid()
    {
        Assert.Equal(["Password"], InvalidFields(new CreateUserRequest("ivan", "1234567", Roles.Manager, null)));
    }

    [Fact]
    public void Password_EightCharacters_IsValid()
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("ivan", "12345678", Roles.Manager, null)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyUserName_IsInvalid(string userName)
    {
        Assert.Equal(["UserName"], InvalidFields(new CreateUserRequest(userName, "password", Roles.Manager, null)));
    }

    [Fact]
    public void UserName_LongerThan50_IsInvalid()
    {
        Assert.Equal(
            ["UserName"],
            InvalidFields(new CreateUserRequest(new string('a', 51), "password", Roles.Manager, null)));
    }

    [Theory]
    [InlineData("иван петров")]
    [InlineData("ivan/petrov")]
    public void UserName_WithSpaceOrOtherSymbol_IsInvalid_WithBulgarianMessage(string userName)
    {
        var errors = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest(userName, "password", Roles.Manager, null))
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
        Assert.Empty(InvalidFields(new CreateUserRequest("иван.петров", "password", Roles.Manager, null)));
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Manager)]
    public void OwnerOrManager_WithPerson_IsValid(string role)
    {
        Assert.Empty(InvalidFields(new CreateUserRequest("ivan", "password", role, null, Guid.NewGuid())));
    }

    [Fact]
    public void ClientViewer_WithPerson_IsInvalid_OnPersonId()
    {
        var errors = new CreateUserRequestValidator()
            .Validate(new CreateUserRequest("zhenistroy", "password", Roles.ClientViewer, CompanyId, Guid.NewGuid()))
            .Errors;

        var error = Assert.Single(errors);
        Assert.Equal("PersonId", error.PropertyName);
        Assert.Equal("Възложителят не може да е човек от екипа.", error.ErrorMessage);
    }
}
