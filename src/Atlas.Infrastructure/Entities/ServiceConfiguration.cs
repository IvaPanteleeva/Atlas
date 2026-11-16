namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;
using Atlas.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    // Longest Unit name is 11 characters; room for later units. Reused by WorkItem in feature 5.
    public const int UnitMaxLength = 20;

    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(ServiceRules.NameMaxLength);

        // Stored as text ("SquareMeter"): readable in the database and safe if the enum is reordered.
        builder.Property(s => s.Unit)
            .HasConversion<string>()
            .HasMaxLength(UnitMaxLength);

        builder.Property(s => s.DefaultRate)
            .HasPrecision(MoneyRules.Precision, MoneyRules.Scale);

        // The same name may exist with another unit („Шпакловка“ per м² and per м.л.).
        // Exact match only: ServiceRepository checks case-insensitively first, the index guards against races.
        builder.HasIndex(s => new { s.Name, s.Unit })
            .IsUnique();
    }
}
