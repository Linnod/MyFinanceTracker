using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.InMemory;

namespace MyFinanceTracker.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInMemoryCategoryPersistence(this IServiceCollection services)
    {
        return services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
    }
}
