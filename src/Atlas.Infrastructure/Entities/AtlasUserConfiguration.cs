namespace Atlas.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AtlasUserConfiguration : IEntityTypeConfiguration<AtlasUser>
{
    public void Configure(EntityTypeBuilder<AtlasUser> builder)
    {
        // A company with users cannot be deleted by accident.
        builder.HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // A person linked to a profile cannot be deleted by accident (PersonRepository answers 409 first).
        builder.HasOne(u => u.Person)
            .WithMany()
            .HasForeignKey(u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // One profile per person; most profiles have none.
        builder.HasIndex(u => u.PersonId)
            .IsUnique()
            .HasFilter("\"PersonId\" IS NOT NULL");
    }
}
