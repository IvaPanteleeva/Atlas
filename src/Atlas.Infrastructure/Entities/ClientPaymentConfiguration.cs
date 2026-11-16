namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;
using Atlas.Application.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ClientPaymentConfiguration : IEntityTypeConfiguration<ClientPayment>
{
    public void Configure(EntityTypeBuilder<ClientPayment> builder)
    {
        // numeric(12,2): up to 1 000 000 with 2 decimals, like the validator (PaymentRules).
        builder.Property(p => p.Amount)
            .HasPrecision(PaymentRules.AmountPrecision, MoneyRules.Scale);

        builder.Property(p => p.Note).HasMaxLength(PaymentRules.NoteMaxLength);

        // A site with payments cannot be deleted by accident (SiteRepository answers 409 first).
        builder.HasOne(p => p.Site)
            .WithMany()
            .HasForeignKey(p => p.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // The list of a site, newest first. Also serves the FK and the sums per site.
        builder.HasIndex(p => new { p.SiteId, p.Date });
    }
}
