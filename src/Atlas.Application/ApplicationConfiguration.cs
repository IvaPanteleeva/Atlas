namespace Atlas.Application;

using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validation messages in Bulgarian (FluentValidation's built-in BulgarianLanguage).
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("bg");

        services.AddValidatorsFromAssembly(typeof(ApplicationConfiguration).Assembly);

        return services;
    }
}
