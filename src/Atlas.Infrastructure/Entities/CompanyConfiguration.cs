namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    // Same limit as the validators (Application cannot see this class).
    public const int NameMaxLength = CompanyRules.NameMaxLength;

    public const string ZhenistroyName = "ЖЕНИСТРОЙ ЕООД";

    // Fixed so the seed row has the same Id in every database.
    public static readonly Guid ZhenistroyId = new("8c5c1efe-7ce1-4838-9c53-1aff142a5b2a");

    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasData(new Company { Id = ZhenistroyId, Name = ZhenistroyName });
    }
}
