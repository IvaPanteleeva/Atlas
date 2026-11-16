using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class SecurityStampTests(AtlasApiFactory factory)
{
    // A new password or a deleted profile ends the user's other sessions within a minute
    // (the cookie is checked against the security stamp at most a minute after the last check).
    [Fact]
    public void Cookie_IsCheckedAgainstTheSecurityStamp_EveryMinute()
    {
        var options = factory.Services.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(1), options.ValidationInterval);
    }
}
