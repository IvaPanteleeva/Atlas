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
    }
}
