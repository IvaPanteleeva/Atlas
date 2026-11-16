namespace Atlas.Infrastructure.Configuration;

using Atlas.Application.Contracts;
using Atlas.Infrastructure.Entities;
using Atlas.Infrastructure.Identity;
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

    // Users, roles and sign-in. The cookie scheme itself is set up in Atlas.Api (AddAtlasAuth):
    // SignInManager needs those authentication services at runtime.
    private static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AtlasUser>(IdentityOptionsSetup.Apply)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AtlasDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<AtlasClaimsPrincipalFactory>();

        services.AddScoped<IdentitySeeder>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
