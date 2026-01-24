using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

public static class DependencyInjection
{
    public static IServiceCollection AddYamlCategoryRepository(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<YamlPersistenceOptions>()
            .Bind(configuration.GetSection("YamlPersistence"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ICategoryRepository, YamlCategoryRepository>();

        return services;
    }
}
