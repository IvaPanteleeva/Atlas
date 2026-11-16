namespace Atlas.Infrastructure.Entities;

using Atlas.Application.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        // "People", not EF's default "Persons".
        builder.ToTable("People");

        // Lengths are the same as the validator (PersonRules). Names may repeat: no unique index.
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(PersonRules.NameMaxLength);
        builder.Property(p => p.Phone).HasMaxLength(PersonRules.PhoneMaxLength);
        builder.Property(p => p.Note).HasMaxLength(PersonRules.NoteMaxLength);
    }
}
