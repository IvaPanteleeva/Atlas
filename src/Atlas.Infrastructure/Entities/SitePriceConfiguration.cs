namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SitePriceConfiguration : IEntityTypeConfiguration<SitePrice>
{
    public void Configure(EntityTypeBuilder<SitePrice> builder)
    {
        builder.Property(p => p.Rate)
            .HasPrecision(MoneyRules.Precision, MoneyRules.Scale);

        // Deleting a site deletes its prices: they are agreements, not history.
        builder.HasOne(p => p.Site)
            .WithMany()
            .HasForeignKey(p => p.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // A service with prices cannot be deleted by accident (ServiceRepository answers 409 first).
        builder.HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // One price per service on a site. Also serves the lookups by site.
        builder.HasIndex(p => new { p.SiteId, p.ServiceId })
            .IsUnique();

        builder.HasIndex(p => p.ServiceId);
    }
}
