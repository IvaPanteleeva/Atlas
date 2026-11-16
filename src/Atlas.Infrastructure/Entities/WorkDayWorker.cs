namespace Atlas.Infrastructure.Entities;

// Who worked on a work day: many-to-many WorkDay ↔ Person by id, so renaming a person keeps the history
// (CLAUDE.md "Решения спрямо прототипа"). Needed later for paying people per day.
public class WorkDayWorker
{
    public Guid WorkDayId { get; set; }

    public WorkDay? WorkDay { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }
}
