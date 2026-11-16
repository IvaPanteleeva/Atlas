namespace Atlas.Infrastructure.Configuration;

using Atlas.Application.Contracts;
using Atlas.Infrastructure.Entities;
using Atlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddDatabase(configuration)
            .AddRepositories()
            .AddIdentity();
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddDbContext<AtlasDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Atlas")));
    }

    // Every class implementing IRepository<T> is registered against its matching interface
    // (CompanyRepository -> ICompanyRepository), scoped like the DbContext it uses.
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .Scan(scan => scan
                .FromAssemblyOf<AtlasDbContext>()
                .AddClasses(classes => classes.AssignableTo(typeof(IRepository<>)))
                .AsMatchingInterface()
                .WithScopedLifetime());
    }

    // Users and roles only. AddSignInManager comes in step 2.2 together with the cookie:
    // SignInManager needs the authentication services, without them DI validation fails at startup.
    private static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AtlasUser>(IdentityOptionsSetup.Apply)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AtlasDbContext>();

        services.AddScoped<IdentitySeeder>();

        return services;
    }
}
