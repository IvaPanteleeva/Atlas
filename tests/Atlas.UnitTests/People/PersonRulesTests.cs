using Atlas.Application.People;

namespace Atlas.UnitTests.People;

public class PersonRulesTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);

    [Fact]
    public void NormalizeName_Trims()
    {
        Assert.Equal("Иван Петров", PersonRules.NormalizeName("  Иван Петров  "));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" 0888 123 456 ", "0888 123 456")]
    public void NormalizeOptional_TrimsAndTurnsEmptyIntoNull(string? value, string? expected)
    {
        Assert.Equal(expected, PersonRules.NormalizeOptional(value));
    }

    [Fact]
    public void WorkStats_WithoutDays_IsZeroAndNoDate()
    {
        Assert.Equal((0, (DateOnly?)null), PersonRules.WorkStats([]));
    }

    // Two sites on the same date are one day of work for the person (CLAUDE.md "Хора и профили").
    [Fact]
    public void WorkStats_CountsDistinctDates_AndTakesTheLatest()
    {
        var stats = PersonRules.WorkStats([Monday, Monday.AddDays(2), Monday, Monday.AddDays(1), Monday.AddDays(2)]);

        Assert.Equal((3, (DateOnly?)Monday.AddDays(2)), stats);
    }

    [Fact]
    public void WorkStats_TwoSitesOnOneDate_IsOneDay()
    {
        Assert.Equal((1, (DateOnly?)Monday), PersonRules.WorkStats([Monday, Monday]));
    }
}
