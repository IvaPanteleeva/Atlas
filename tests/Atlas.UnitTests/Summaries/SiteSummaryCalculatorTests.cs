using System.Globalization;
using Atlas.Application.Summaries;

namespace Atlas.UnitTests.Summaries;

// The sums of CLAUDE.md "Плащания и суми": one rule for the day, the site and the overall bar.
public class SiteSummaryCalculatorTests
{
    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static SummaryItem Item(string quantity, string? rate, bool needsReview = false)
        => new(D(quantity), rate is null ? null : D(rate), needsReview);

    [Fact]
    public void Empty_IsAllZero()
    {
        var summary = SiteSummaryCalculator.Calculate([], []);

        Assert.Equal(new SiteSummary(0m, 0m, 0m, 0), summary);
    }

    [Fact]
    public void ItemWithoutPrice_IsNotInTheSum_ButIsCounted()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("10", "7.50"), Item("1", null)], []);

        Assert.Equal(75m, summary.Earned);
        Assert.Equal(1, summary.OpenCount);
    }

    [Fact]
    public void FlaggedItemWithPrice_IsInTheSum_AndIsCounted()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("5", "6.00", needsReview: true)], []);

        Assert.Equal(30m, summary.Earned);
        Assert.Equal(1, summary.OpenCount);
    }

    [Fact]
    public void FlaggedItemWithoutPrice_IsCountedOnce()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("1", null, needsReview: true)], []);

        Assert.Equal(0m, summary.Earned);
        Assert.Equal(1, summary.OpenCount);
    }

    [Theory]
    [InlineData("1.333", "7.50", "10.00")] // 9.9975
    [InlineData("0.125", "1", "0.13")] // midpoint away from zero, not to even (0.12)
    public void EachItem_IsRoundedToCents(string quantity, string rate, string expected)
    {
        var summary = SiteSummaryCalculator.Calculate([Item(quantity, rate)], []);

        Assert.Equal(D(expected), summary.Earned);
    }

    // Rounded per item, then summed: 0.13 + 0.13 = 0.26, not round(0.125 + 0.125) = 0.25.
    [Fact]
    public void Earned_IsTheSumOfTheRoundedItems()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("0.125", "1"), Item("0.125", "1")], []);

        Assert.Equal(0.26m, summary.Earned);
    }

    [Fact]
    public void Paid_IsTheSumOfThePayments_RemainingIsEarnedMinusPaid()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("10", "7.50"), Item("5", "6.00")], [50m, 20m]);

        Assert.Equal(new SiteSummary(105m, 70m, 35m, 0), summary);
    }

    [Fact]
    public void Overpaid_RemainingIsNegative()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("10", "7.50")], [50m, 50m]);

        Assert.Equal(100m, summary.Paid);
        Assert.Equal(-25m, summary.Remaining);
    }

    [Fact]
    public void PaymentsWithoutWork_RemainingIsNegative()
    {
        var summary = SiteSummaryCalculator.Calculate([], [40m]);

        Assert.Equal(new SiteSummary(0m, 40m, -40m, 0), summary);
    }

    // The site's sum equals the sum of its days' sums (both are sums of the same rounded items).
    [Fact]
    public void SiteEarned_EqualsTheSumOfTheDayTotals()
    {
        SummaryItem[] day1 = [Item("1.333", "7.50"), Item("0.125", "1"), Item("2", null)];
        SummaryItem[] day2 = [Item("0.125", "1"), Item("12.5", "6.55", needsReview: true)];

        var site = SiteSummaryCalculator.Calculate([.. day1, .. day2], []);
        var first = SiteSummaryCalculator.Calculate(day1, []);
        var second = SiteSummaryCalculator.Calculate(day2, []);

        Assert.Equal(first.Earned + second.Earned, site.Earned);
        Assert.Equal(first.OpenCount + second.OpenCount, site.OpenCount);
        Assert.Equal(10.00m + 0.13m + 0.13m + 81.88m, site.Earned);
    }

    // CLAUDE.md "Плащания и суми": „Дължат ни“ sums only the positive remainders; an overpaid site does not lower
    // the debt of another one, it is shown apart („Надплатено X € по N обекта“).
    [Fact]
    public void Total_OwedIgnoresOverpaidSites_OverpaidIsSeparate()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(400m, 100m, 300m, 2),
            new SiteSummary(110m, 0m, 110m, 1),
            new SiteSummary(105m, 120m, -15m, 0),
        ]);

        Assert.Equal(new SummaryTotals(615m, 220m, 410m, 15m, 1, 3), total);
    }

    [Fact]
    public void Total_AllPaidExactly_OwedAndOverpaidAreZero()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(105m, 105m, 0m, 0),
            new SiteSummary(20m, 20m, 0m, 1),
        ]);

        Assert.Equal(new SummaryTotals(125m, 125m, 0m, 0m, 0, 1), total);
    }

    [Fact]
    public void Total_SeveralOverpaidSites_AreAddedAndCounted()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(10m, 40m, -30m, 0),
            new SiteSummary(105m, 120m, -15m, 0),
            new SiteSummary(50m, 0m, 50m, 0),
        ]);

        Assert.Equal(new SummaryTotals(165m, 160m, 50m, 45m, 2, 0), total);
    }

    [Fact]
    public void Total_OfNoSites_IsAllZero()
    {
        Assert.Equal(new SummaryTotals(0m, 0m, 0m, 0m, 0, 0), SiteSummaryCalculator.Total([]));
    }
}
