namespace Atlas.Infrastructure.Entities;

// Who worked on a work day: many-to-many WorkDay ↔ Person by id, so renaming a person keeps the history
// (CLAUDE.md "Решения спрямо прототипа"). Needed later for paying people per day.
public class WorkDayWorker
{
    public Guid WorkDayId { get; set; }

    public WorkDay? WorkDay { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    // What the person gets for this day: a copy (filled from the daily rate by the form, may be changed).
    // Null = „за уточняване“: not in the worker cost, counted apart (CLAUDE.md "Разход за хора").
    public decimal? Pay { get; set; }
}
