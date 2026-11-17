namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SiteTaskConfiguration : IEntityTypeConfiguration<SiteTask>
{
    public void Configure(EntityTypeBuilder<SiteTask> builder)
    {
        // The same length as the validator (TaskRules).
        builder.Property(t => t.Text)
            .IsRequired()
            .HasMaxLength(TaskRules.TextMaxLength);

        // Tasks are not history: deleting a site deletes its tasks and they never block it.
        builder.HasOne(t => t.Site)
            .WithMany()
            .HasForeignKey(t => t.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting the person responsible leaves the task without one (CLAUDE.md "Задачи").
        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.SiteId);
        builder.HasIndex(t => t.AssigneeId);
    }
}
