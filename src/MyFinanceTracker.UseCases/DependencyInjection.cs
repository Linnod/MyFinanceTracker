using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Category.List.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Create.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Create.Validation;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Delete.Validation;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Get.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Get.Validation;

namespace MyFinanceTracker.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            })
            .AddTransactionUseCases()
            .AddCategoriesUseCases();
    }

    private static IServiceCollection AddTransactionUseCases(this IServiceCollection services)
    {
        return services
            .AddTransactionCreate()
            .AddTransactionDelete()
            .AddTransactionGet();
    }

    private static IServiceCollection AddTransactionCreate(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<CreateTransactionsRequest, CreateTransactionsResponse>,
                CreateTransactionsLoggingBehavior>()
            .AddScoped<IValidator<CreateTransactionsRequest>, CreateTransactionsRequestValidator>()
            .AddTransient<
                IPipelineBehavior<CreateTransactionsRequest, CreateTransactionsResponse>,
                CreateTransactionsValidationBehavior>();
    }

    private static IServiceCollection AddTransactionDelete(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>,
                DeleteTransactionsLoggingBehavior>()
            .AddScoped<IValidator<DeleteTransactionsRequest>, DeleteTransactionsRequestValidator>()
            .AddTransient<
                IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>,
                DeleteTransactionValidationBehavior>();
    }

    private static IServiceCollection AddTransactionGet(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<GetTransactionsRequest, GetTransactionsResponse>,
                GetTransactionsLoggingBehavior>()
            .AddScoped<IValidator<GetTransactionsRequest>, GetTransactionsRequestValidator>()
            .AddTransient<
                IPipelineBehavior<GetTransactionsRequest, GetTransactionsResponse>,
                GetTransactionsValidationBehavior>();
    }

    private static IServiceCollection AddCategoriesUseCases(this IServiceCollection services)
    {
        return services.AddListCategories();
    }

    private static IServiceCollection AddListCategories(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<ListCategoriesRequest, ListCategoriesResponse>,
                ListCategoriesLoggingBehavior>();
    }
}