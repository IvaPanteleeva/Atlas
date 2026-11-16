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

    public DbSet<Service> Services => this.Set<Service>();

    public DbSet<SitePrice> SitePrices => this.Set<SitePrice>();

    public DbSet<WorkDay> WorkDays => this.Set<WorkDay>();

    public DbSet<WorkItem> WorkItems => this.Set<WorkItem>();

    public DbSet<ClientPayment> ClientPayments => this.Set<ClientPayment>();

    public DbSet<Person> People => this.Set<Person>();

    public DbSet<WorkDayWorker> WorkDayWorkers => this.Set<WorkDayWorker>();

    public DbSet<SiteContact> SiteContacts => this.Set<SiteContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own model first, so our configurations can extend it.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> (the *Configuration.cs next to each entity).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtlasDbContext).Assembly);
    }
}
