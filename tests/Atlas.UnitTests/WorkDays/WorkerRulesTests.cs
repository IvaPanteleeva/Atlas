using Atlas.Application.WorkDays;

namespace Atlas.UnitTests.WorkDays;

// Who may be put on a work day (CLAUDE.md "Хора и профили"): new workers must be crew,
// people already on the day stay even when they are no longer crew.
public class WorkerRulesTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void Allowed_ReturnsNoError(bool isOnTheDay, bool isCrew)
    {
        Assert.Null(WorkDayRules.WorkerError(isOnTheDay, isCrew));
    }

    [Fact]
    public void NewWorker_NotCrew_IsRejected()
    {
        Assert.Equal(WorkDayRules.WorkerNotCrewMessage, WorkDayRules.WorkerError(isOnTheDay: false, isCrew: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownPerson_IsRejected(bool isOnTheDay)
    {
        Assert.Equal(WorkDayRules.WorkerNotFoundMessage, WorkDayRules.WorkerError(isOnTheDay, isCrew: null));
    }

    [Fact]
    public void Messages_AreBulgarian()
    {
        Assert.Equal("Няма такъв човек.", WorkDayRules.WorkerNotFoundMessage);
        Assert.Equal("Човекът не е от екипа.", WorkDayRules.WorkerNotCrewMessage);
    }
}
