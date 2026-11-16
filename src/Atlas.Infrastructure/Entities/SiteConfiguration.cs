namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    // Longest SiteStatus name is 6 characters; room for later statuses.
    public const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<Site> builder)
    {
        // Lengths are the same as the validators (SiteRules).
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(SiteRules.NameMaxLength);

        // Stored as text ("Lead", "Active", "Done"): readable in the database and safe if the enum is reordered.
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength);

        builder.Property(s => s.EndCustomer).HasMaxLength(SiteRules.EndCustomerMaxLength);
        builder.Property(s => s.Address).HasMaxLength(SiteRules.AddressMaxLength);
        builder.Property(s => s.MapUrl).HasMaxLength(SiteRules.MapUrlMaxLength);
        builder.Property(s => s.Note).HasMaxLength(SiteRules.NoteMaxLength);

        // A company with sites cannot be deleted by accident (CompanyRepository answers 409 first).
        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.CompanyId);
    }
}
