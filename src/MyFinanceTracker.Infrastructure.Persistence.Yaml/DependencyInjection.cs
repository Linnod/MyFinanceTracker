using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

public static class DependencyInjection
{
    public static IServiceCollection AddYamlCategoryRepository(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<YamlPersistenceOptions>()
            .Bind(configuration.GetSection("YamlPersistence"))
            .ValidateDataAnnotations()
            .Validate(options => File.Exists(options.FilePath), "Category YAML file not found. Check your environment or volume mounts.")
            .ValidateOnStart();
        services.AddSingleton<ICategoryRepository, YamlCategoryRepository>()
            .AddSingleton<ICategoryLoader, YamlCategoryLoader>();

        return services;
    }
}
