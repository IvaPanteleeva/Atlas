using Atlas.Application.Companies;

namespace Atlas.UnitTests.Companies;

public class CompanyRulesTests
{
    [Theory]
    [InlineData(" Фирма Х ", "Фирма Х")]
    [InlineData("Фирма Х", "Фирма Х")]
    [InlineData("\tФирма  Х\n", "Фирма  Х")]
    public void NormalizeName_TrimsOnlyTheEnds(string name, string expected)
    {
        Assert.Equal(expected, CompanyRules.NormalizeName(name));
    }
}
