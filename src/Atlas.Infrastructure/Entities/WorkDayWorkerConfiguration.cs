namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class WorkDayWorkerConfiguration : IEntityTypeConfiguration<WorkDayWorker>
{
    public void Configure(EntityTypeBuilder<WorkDayWorker> builder)
    {
        // A person is on a day at most once.
        builder.HasKey(w => new { w.WorkDayId, w.PersonId });

        builder.Property(w => w.Pay).HasPrecision(MoneyRules.Precision, MoneyRules.Scale);

        // The links belong to the day: deleting the day deletes them, not the people.
        builder.HasOne(w => w.WorkDay)
            .WithMany(d => d.Workers)
            .HasForeignKey(w => w.WorkDayId)
            .OnDelete(DeleteBehavior.Cascade);

        // A person with work days cannot be deleted (PersonRepository answers 409 first).
        builder.HasOne(w => w.Person)
            .WithMany()
            .HasForeignKey(w => w.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Days per person; also serves the FK (the key starts with WorkDayId).
        builder.HasIndex(w => w.PersonId);
    }
}
