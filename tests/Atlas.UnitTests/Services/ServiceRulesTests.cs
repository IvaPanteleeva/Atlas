using Atlas.Application.Services;

namespace Atlas.UnitTests.Services;

public class ServiceRulesTests
{
    [Theory]
    [InlineData(" Шпакловка ", "Шпакловка")]
    [InlineData("Шпакловка", "Шпакловка")]
    [InlineData("\tШпакловка  таван\n", "Шпакловка  таван")]
    public void NormalizeName_TrimsOnlyTheEnds(string name, string expected)
    {
        Assert.Equal(expected, ServiceRules.NormalizeName(name));
    }

    [Theory]
    [InlineData(1, "Услугата има цени в 1 обект")]
    [InlineData(2, "Услугата има цени в 2 обекта")]
    [InlineData(11, "Услугата има цени в 11 обекта")]
    public void InUseMessage_SaysInHowManySites(int count, string expected)
    {
        Assert.Equal(expected, ServiceRules.InUseMessage(count));
    }
}
