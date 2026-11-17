using System.Globalization;
using Atlas.Application.Summaries;

namespace Atlas.UnitTests.Summaries;

// The sums of CLAUDE.md "Плащания и суми": one rule for the day, the site and the overall bar.
public class SiteSummaryCalculatorTests
{
    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static SummaryItem Item(string quantity, string? rate, bool needsReview = false)
        => new(D(quantity), rate is null ? null : D(rate), needsReview);

    private static SummaryWorker W(decimal? pay, bool isPartner = false)
        => new(pay, isPartner);

    [Fact]
    public void Empty_IsAllZero()
    {
        var summary = SiteSummaryCalculator.Calculate([], []);

        Assert.Equal(new SiteSummary(0m, 0m, 0m, 0, 0m, 0, 0m), summary);
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

        Assert.Equal(new SiteSummary(105m, 70m, 35m, 0, 0m, 0, 105m), summary);
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

        Assert.Equal(new SiteSummary(0m, 40m, -40m, 0, 0m, 0, 0m), summary);
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
            new SiteSummary(400m, 100m, 300m, 2, 0m, 0, 400m),
            new SiteSummary(110m, 0m, 110m, 1, 0m, 0, 110m),
            new SiteSummary(105m, 120m, -15m, 0, 0m, 0, 105m),
        ]);

        Assert.Equal(new SummaryTotals(615m, 220m, 410m, 15m, 1, 3, 0m, 0, 615m), total);
    }

    [Fact]
    public void Total_AllPaidExactly_OwedAndOverpaidAreZero()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(105m, 105m, 0m, 0, 0m, 0, 105m),
            new SiteSummary(20m, 20m, 0m, 1, 0m, 0, 20m),
        ]);

        Assert.Equal(new SummaryTotals(125m, 125m, 0m, 0m, 0, 1, 0m, 0, 125m), total);
    }

    [Fact]
    public void Total_SeveralOverpaidSites_AreAddedAndCounted()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(10m, 40m, -30m, 0, 0m, 0, 10m),
            new SiteSummary(105m, 120m, -15m, 0, 0m, 0, 105m),
            new SiteSummary(50m, 0m, 50m, 0, 0m, 0, 50m),
        ]);

        Assert.Equal(new SummaryTotals(165m, 160m, 50m, 45m, 2, 0, 0m, 0, 165m), total);
    }

    [Fact]
    public void Total_OfNoSites_IsAllZero()
    {
        Assert.Equal(new SummaryTotals(0m, 0m, 0m, 0m, 0, 0, 0m, 0, 0m), SiteSummaryCalculator.Total([]));
    }

    // CLAUDE.md "Разход за хора": the pay of each person-day; an empty pay is not in the cost but is counted.
    [Fact]
    public void WorkerCost_SumsThePays_EmptyPayIsCountedApart_NetIsEarnedMinusCost()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("1", "105")], [50m], [W(80m), W(null)]);

        Assert.Equal(new SiteSummary(105m, 50m, 55m, 0, 80m, 1, 25m), summary);
    }

    // „Остава за нас“ comes from Earned, not from Paid, and may be negative.
    [Fact]
    public void Net_MayBeNegative_AndDoesNotDependOnPayments()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("1", "100")], [100m], [W(80m), W(70.50m)]);

        Assert.Equal((150.50m, 0, -50.50m), (summary.WorkerCost, summary.WorkerOpenCount, summary.Net));
    }

    [Fact]
    public void WithoutWorkers_NetIsEarned()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("2", "10")], [], []);

        Assert.Equal((0m, 0, 20m), (summary.WorkerCost, summary.WorkerOpenCount, summary.Net));
    }

    [Fact]
    public void Total_SumsWorkerCostOpenCountAndNet_NegativeNetLowersTheTotal()
    {
        var total = SiteSummaryCalculator.Total(
        [
            new SiteSummary(105m, 0m, 105m, 0, 80m, 1, 25m),
            new SiteSummary(100m, 0m, 100m, 0, 150m, 0, -50m),
            new SiteSummary(0m, 0m, 0m, 0, 0m, 2, 0m),
        ]);

        Assert.Equal((230m, 3, -25m), (total.WorkerCost, total.WorkerOpenCount, total.Net));
    }

    // Partners („съдружници“) have no pay: they are not „за уточняване“. „Остава за нас“ is their profit.
    [Fact]
    public void Partner_WithoutPay_IsNotCountedAsOpen_NorAsCost()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("1", "105")], [], [W(80m), W(null, isPartner: true), W(null)]);

        Assert.Equal((80m, 1, 25m), (summary.WorkerCost, summary.WorkerOpenCount, summary.Net));
    }

    // Someone who became a partner later keeps the pay stored on their old days (a copy): it stays in the cost.
    [Fact]
    public void Partner_WithAPayStoredBefore_StaysInTheCost()
    {
        var summary = SiteSummaryCalculator.Calculate([Item("1", "105")], [], [W(50m, isPartner: true)]);

        Assert.Equal((50m, 0, 55m), (summary.WorkerCost, summary.WorkerOpenCount, summary.Net));
    }

    // A partner's pay for the day („надник“, рунд 31) is optional: filled in, it is a cost like a worker's;
    // empty, it is not „за уточняване“.
    [Fact]
    public void Partner_WithAPay_IsCost_WithoutPay_IsNotOpen()
    {
        var summary = SiteSummaryCalculator.Calculate(
            [Item("3", "150")],
            [],
            [W(150m, isPartner: true), W(null, isPartner: true), W(120m)]);

        Assert.Equal((270m, 0, 180m), (summary.WorkerCost, summary.WorkerOpenCount, summary.Net));
    }
}
