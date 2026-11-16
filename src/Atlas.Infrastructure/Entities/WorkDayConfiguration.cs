namespace Atlas.Infrastructure.Entities;

using Atlas.Application.WorkDays;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class WorkDayConfiguration : IEntityTypeConfiguration<WorkDay>
{
    public void Configure(EntityTypeBuilder<WorkDay> builder)
    {
        // Lengths are the same as the validators (WorkDayRules).
        builder.Property(d => d.Title).HasMaxLength(WorkDayRules.TitleMaxLength);
        builder.Property(d => d.Note).HasMaxLength(WorkDayRules.NoteMaxLength);

        // A site with work days cannot be deleted by accident (SiteRepository answers 409 first).
        builder.HasOne(d => d.Site)
            .WithMany()
            .HasForeignKey(d => d.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // The items belong to the day: deleting the day deletes them.
        builder.HasMany(d => d.Items)
            .WithOne(i => i.WorkDay)
            .HasForeignKey(i => i.WorkDayId)
            .OnDelete(DeleteBehavior.Cascade);

        // The list of a site, newest first. Also serves the FK.
        builder.HasIndex(d => new { d.SiteId, d.Date });
    }
}
