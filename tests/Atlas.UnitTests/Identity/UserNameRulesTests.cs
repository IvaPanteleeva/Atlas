using Atlas.Infrastructure.Configuration;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Atlas.UnitTests.Identity;

// Identity's own UserValidator with the allowed user name characters from IdentityOptionsSetup.
public class UserNameRulesTests
{
    [Theory]
    [InlineData("иван")]
    [InlineData("Иван.Петров")]
    [InlineData("бригада-2")]
    [InlineData("ivan_petrov")]
    [InlineData("ivan@atlas.bg")]
    public async Task LatinOrCyrillicLetters_DigitsAndSeparators_AreAccepted(string userName)
    {
        var result = await ValidateAsync(userName);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Code)));
    }

    [Theory]
    [InlineData("иван петров")]
    [InlineData("ivan/petrov")]
    public async Task SpacesAndOtherSymbols_AreRejected(string userName)
    {
        var result = await ValidateAsync(userName);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidUserName));
    }

    private static async Task<IdentityResult> ValidateAsync(string userName)
    {
        var options = new IdentityOptions();
        IdentityOptionsSetup.Apply(options);

        using var userManager = new UserManager<AtlasUser>(
            new FakeUserStore(),
            Options.Create(options),
            new PasswordHasher<AtlasUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new FakeServiceProvider(),
            NullLogger<UserManager<AtlasUser>>.Instance);

        return await new UserValidator<AtlasUser>().ValidateAsync(userManager, new AtlasUser { UserName = userName });
    }

    // Empty store: the user name is read from the user, and nobody else has it.
    private sealed class FakeUserStore : IUserStore<AtlasUser>
    {
        public Task<string?> GetUserNameAsync(AtlasUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public Task<AtlasUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<AtlasUser?>(null);

        public Task<string> GetUserIdAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetUserNameAsync(AtlasUser user, string? userName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(AtlasUser user, string? normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AtlasUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
