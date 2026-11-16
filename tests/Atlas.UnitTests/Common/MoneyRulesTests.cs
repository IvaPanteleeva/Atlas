using Atlas.Application.Common;

namespace Atlas.UnitTests.Common;

public class MoneyRulesTests
{
    [Theory]
    [InlineData("6")]
    [InlineData("6.5")]
    [InlineData("6.55")]
    [InlineData("6.550")]
    [InlineData("0.01")]
    [InlineData("100000")]
    public void HasAtMostTwoDecimals_True(string amount)
    {
        Assert.True(MoneyRules.HasAtMostTwoDecimals(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("6.555")]
    [InlineData("0.001")]
    [InlineData("99999.999")]
    public void HasAtMostTwoDecimals_False(string amount)
    {
        Assert.False(MoneyRules.HasAtMostTwoDecimals(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }
}
