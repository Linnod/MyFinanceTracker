using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

public static class DependencyInjection
{
    public static IServiceCollection AddYamlCategoryRepository(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<YamlPersistenceOptions>()
            .Bind(configuration.GetSection("YamlPersistence"))
            .ValidateDataAnnotations()
            .Validate(options => File.Exists(options.FilePath), "Category file not found.")
            .ValidateOnStart();

        services.AddSingleton<ICategoryParser, YamlCategoryParser>()
            .AddSingleton<ICategoryLoader, YamlCategoryLoader>()
            .AddSingleton<ICategoryRepository, YamlCategoryRepository>();

        return services;
    }
}
