namespace Atlas.Application;

using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validation messages in Bulgarian (FluentValidation's built-in BulgarianLanguage).
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("bg");

        // The clock for "today" (work day dates); the tests can replace it.
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssembly(typeof(ApplicationConfiguration).Assembly);

        return services;
    }
}
