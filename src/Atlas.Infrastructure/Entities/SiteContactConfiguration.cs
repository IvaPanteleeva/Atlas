namespace Atlas.Infrastructure.Entities;

using Atlas.Application.SiteContacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SiteContactConfiguration : IEntityTypeConfiguration<SiteContact>
{
    public void Configure(EntityTypeBuilder<SiteContact> builder)
    {
        // Lengths are the same as the validator (SiteContactRules).
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(SiteContactRules.NameMaxLength);
        builder.Property(c => c.Role).HasMaxLength(SiteContactRules.RoleMaxLength);
        builder.Property(c => c.Phone).HasMaxLength(SiteContactRules.PhoneMaxLength);
        builder.Property(c => c.Note).HasMaxLength(SiteContactRules.NoteMaxLength);

        // Contacts are not history: deleting a site deletes them and they never block it.
        builder.HasOne(c => c.Site)
            .WithMany()
            .HasForeignKey(c => c.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.SiteId);
    }
}
