namespace Atlas.Infrastructure.Persistence;

using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// Identity tables keep their default names (AspNetUsers, AspNetRoles, ...).
public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options)
    : IdentityDbContext<AtlasUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Company> Companies => this.Set<Company>();

    public DbSet<Site> Sites => this.Set<Site>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own model first, so our configurations can extend it.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> (the *Configuration.cs next to each entity).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtlasDbContext).Assembly);
    }
}
