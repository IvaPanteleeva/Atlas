using Atlas.Infrastructure.Configuration;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Atlas.UnitTests.Identity;

public class PasswordRulesTests
{
    [Fact]
    public async Task SevenCharacters_AreRejected()
    {
        var result = await ValidateAsync("abcdefg");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(IdentityErrorDescriber.PasswordTooShort));
    }

    [Fact]
    public async Task EightLowercaseLetters_WithoutDigitsUppercaseOrSymbols_AreAccepted()
    {
        var result = await ValidateAsync("abcdefgh");

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Code)));
    }

    private static async Task<IdentityResult> ValidateAsync(string password)
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

        return await new PasswordValidator<AtlasUser>().ValidateAsync(userManager, new AtlasUser(), password);
    }

    // UserManager needs a store; the password validator never touches it.
    private sealed class FakeUserStore : IUserStore<AtlasUser>
    {
        public Task<string> GetUserIdAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> GetUserNameAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetUserNameAsync(AtlasUser user, string? userName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(AtlasUser user, string? normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(AtlasUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AtlasUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AtlasUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
