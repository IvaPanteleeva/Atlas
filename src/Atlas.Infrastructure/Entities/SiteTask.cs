namespace Atlas.Infrastructure.Entities;

// A task (CLAUDE.md "Задачи"): of a site, or of the company when SiteId is null. Not history: deleting the site
// deletes its tasks; deleting the person responsible leaves the task without one.
public class SiteTask
{
    public Guid Id { get; set; }

    public required string Text { get; set; }

    public bool Done { get; private set; }

    public Guid? SiteId { get; set; }

    public Site? Site { get; set; }

    public Guid? AssigneeId { get; set; }

    public Person? Assignee { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DoneAt { get; private set; }

    // Ticks the task off at this moment. Ticking an already ticked task keeps the first moment
    // (the list orders the ticked tasks by it).
    public void Complete(DateTimeOffset now)
    {
        if (this.Done)
        {
            return;
        }

        this.Done = true;
        this.DoneAt = now;
    }

    // Takes the tick away: the task is open again, without a moment of completion.
    public void Reopen()
    {
        this.Done = false;
        this.DoneAt = null;
    }
}
