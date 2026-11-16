using System.Globalization;
using Atlas.Application.WorkDays;
using Atlas.UnitTests.Fakes;

namespace Atlas.UnitTests.WorkDays;

public class WorkDayRulesTests
{
    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    // Bulgaria is UTC+2 in winter and UTC+3 in summer: a day entered late in the evening is already "tomorrow" in UTC terms
    // only for us, so "today" must be the Sofia date, not the UTC date.
    [Theory]
    [InlineData("2026-11-16T10:00:00Z", "2026-11-16")]
    [InlineData("2026-11-16T21:59:59Z", "2026-11-16")]
    [InlineData("2026-11-16T22:00:00Z", "2026-11-17")]
    [InlineData("2026-07-01T20:59:59Z", "2026-07-01")]
    [InlineData("2026-07-01T21:00:00Z", "2026-07-02")]
    public void Today_IsTheDateInSofia(string utcNow, string expected)
    {
        var today = WorkDayRules.Today(new FixedTimeProvider(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture)));

        Assert.Equal(DateOnly.Parse(expected, CultureInfo.InvariantCulture), today);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("1.5", true)]
    [InlineData("1.333", true)]
    [InlineData("1.3330", true)]
    [InlineData("1.3335", false)]
    [InlineData("0.0001", false)]
    public void HasAtMostThreeDecimals(string quantity, bool expected)
    {
        Assert.Equal(expected, WorkDayRules.HasAtMostThreeDecimals(D(quantity)));
    }

    [Theory]
    [InlineData("2", "10", "20")]
    [InlineData("1.333", "7.50", "10.00")] // 9.9975
    [InlineData("0.125", "1", "0.13")] // midpoint: away from zero, not to even (0.12)
    [InlineData("0.335", "1", "0.34")]
    [InlineData("12.5", "6.55", "81.88")] // 81.875
    public void Amount_IsQuantityTimesRate_RoundedAwayFromZeroToCents(string quantity, string rate, string expected)
    {
        Assert.Equal(D(expected), WorkDayRules.Amount(D(quantity), D(rate)));
    }

    [Fact]
    public void Amount_WithoutRate_IsNull()
    {
        Assert.Null(WorkDayRules.Amount(3m, null));
    }

    [Fact]
    public void Total_SumsOnlyThePricedAmounts()
    {
        Assert.Equal(25.5m, WorkDayRules.Total([20m, null, 5.5m, null]));
    }

    [Fact]
    public void Total_OfNothingPriced_IsZero()
    {
        Assert.Equal(0m, WorkDayRules.Total([null, null]));
        Assert.Equal(0m, WorkDayRules.Total([]));
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData(null, true, true)]
    [InlineData("5", true, true)]
    [InlineData("5", false, false)]
    public void IsOpen_WhenNoPriceOrFlagged(string? rate, bool needsReview, bool expected)
    {
        Assert.Equal(expected, WorkDayRules.IsOpen(rate is null ? null : D(rate), needsReview));
    }

    [Fact]
    public void ResolveRate_Owner_TakesTheRequestedRate_EvenNull()
    {
        Assert.Equal(9m, WorkDayRules.ResolveRate(true, 9m, isExistingItem: true, storedRate: 5m, sitePrice: 7m, defaultRate: 6m));
        Assert.Null(WorkDayRules.ResolveRate(true, null, isExistingItem: false, storedRate: null, sitePrice: 7m, defaultRate: 6m));
    }

    [Fact]
    public void ResolveRate_Crew_ExistingItem_KeepsTheStoredRate()
    {
        Assert.Equal(5m, WorkDayRules.ResolveRate(false, 9m, isExistingItem: true, storedRate: 5m, sitePrice: 7m, defaultRate: 6m));
        Assert.Null(WorkDayRules.ResolveRate(false, 9m, isExistingItem: true, storedRate: null, sitePrice: 7m, defaultRate: 6m));
    }

    [Fact]
    public void ResolveRate_Crew_NewItem_SitePriceThenDefaultThenNull_IgnoringTheRequest()
    {
        Assert.Equal(7m, WorkDayRules.ResolveRate(false, 9m, isExistingItem: false, storedRate: null, sitePrice: 7m, defaultRate: 6m));
        Assert.Equal(6m, WorkDayRules.ResolveRate(false, 9m, isExistingItem: false, storedRate: null, sitePrice: null, defaultRate: 6m));
        Assert.Null(WorkDayRules.ResolveRate(false, 9m, isExistingItem: false, storedRate: null, sitePrice: null, defaultRate: null));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Плочки  ", "Плочки")]
    public void NormalizeOptional_TrimsAndTurnsEmptyIntoNull(string? value, string? expected)
    {
        Assert.Equal(expected, WorkDayRules.NormalizeOptional(value));
    }
}
