namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;
using Atlas.Application.WorkDays;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(WorkDayRules.DescriptionMaxLength);

        // numeric(10,3): up to 100 000 with 3 decimals, like the validator.
        builder.Property(i => i.Quantity)
            .HasPrecision(WorkDayRules.QuantityPrecision, WorkDayRules.QuantityScale);

        // Stored as text ("SquareMeter"), like Service.Unit.
        builder.Property(i => i.Unit)
            .HasConversion<string>()
            .HasMaxLength(ServiceConfiguration.UnitMaxLength);

        builder.Property(i => i.Rate)
            .HasPrecision(MoneyRules.Precision, MoneyRules.Scale);

        // Only for reference: deleting the service keeps the item and its copied price, the link becomes null
        // (CLAUDE.md "Услуги и цени по обект": work days do not block deleting a service).
        builder.HasOne(i => i.Service)
            .WithMany()
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.ServiceId);
    }
}
