using Atlas.Infrastructure.Entities;

namespace Atlas.UnitTests.Tasks;

public class SiteTaskTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Monday = new(2026, 11, 16, 9, 30, 0, TimeSpan.Zero);

    private static SiteTask NewTask() => new() { Text = "Купи лепило", CreatedAt = Created };

    [Fact]
    public void NewTask_IsOpen()
    {
        var task = NewTask();

        Assert.False(task.Done);
        Assert.Null(task.DoneAt);
    }

    [Fact]
    public void Complete_SetsDoneAndTheMoment()
    {
        var task = NewTask();

        task.Complete(Monday);

        Assert.True(task.Done);
        Assert.Equal(Monday, task.DoneAt);
    }

    // Ticking an already ticked task again (e.g. a second tap) keeps the first moment.
    [Fact]
    public void Complete_Twice_KeepsTheFirstMoment()
    {
        var task = NewTask();
        task.Complete(Monday);

        task.Complete(Monday.AddDays(1));

        Assert.Equal(Monday, task.DoneAt);
    }

    [Fact]
    public void Reopen_ClearsDoneAndTheMoment()
    {
        var task = NewTask();
        task.Complete(Monday);

        task.Reopen();

        Assert.False(task.Done);
        Assert.Null(task.DoneAt);
        Assert.Equal(Created, task.CreatedAt);
    }

    [Fact]
    public void Reopen_ThenComplete_TakesTheNewMoment()
    {
        var task = NewTask();
        task.Complete(Monday);
        task.Reopen();

        task.Complete(Monday.AddDays(1));

        Assert.Equal(Monday.AddDays(1), task.DoneAt);
    }
}
