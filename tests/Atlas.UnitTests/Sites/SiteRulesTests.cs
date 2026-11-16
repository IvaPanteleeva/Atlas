using Atlas.Application.Sites;

namespace Atlas.UnitTests.Sites;

public class SiteRulesTests
{
    [Theory]
    [InlineData(" Баня ", "Баня")]
    [InlineData("Баня", "Баня")]
    [InlineData("\tБаня  горе\n", "Баня  горе")]
    public void NormalizeName_TrimsOnlyTheEnds(string name, string expected)
    {
        Assert.Equal(expected, SiteRules.NormalizeName(name));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("\t\n", null)]
    [InlineData(" Професорът ", "Професорът")]
    [InlineData("ул. „Шипка“ 5", "ул. „Шипка“ 5")]
    public void NormalizeOptional_TrimsAndTurnsEmptyIntoNull(string? value, string? expected)
    {
        Assert.Equal(expected, SiteRules.NormalizeOptional(value));
    }

    [Theory]
    [InlineData("https://maps.google.com/?q=42.69,23.32")]
    [InlineData("http://maps.google.com")]
    [InlineData("HTTPS://maps.app.goo.gl/abc")]
    public void IsHttpUrl_AcceptsAbsoluteHttpAndHttps(string url)
    {
        Assert.True(SiteRules.IsHttpUrl(url));
    }

    [Theory]
    [InlineData("maps.google.com")]
    [InlineData("www.google.com/maps")]
    [InlineData("ftp://x")]
    [InlineData("file:///etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:a@b.bg")]
    [InlineData("http://")]
    [InlineData("")]
    public void IsHttpUrl_RejectsEverythingElse(string url)
    {
        Assert.False(SiteRules.IsHttpUrl(url));
    }
}
