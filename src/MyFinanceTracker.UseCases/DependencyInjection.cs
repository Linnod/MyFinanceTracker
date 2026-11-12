using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.UseCases.Behaviors;
using MyFinanceTracker.UseCases.Category.Create;
using MyFinanceTracker.UseCases.Category.Create.Behaviors;
using MyFinanceTracker.UseCases.Category.Create.Validation;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Category.List.Behaviors;
using MyFinanceTracker.UseCases.Category.Update;
using MyFinanceTracker.UseCases.Category.Update.Behaviors;
using MyFinanceTracker.UseCases.Category.Update.Validation;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Create.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Create.Validation;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Delete.Validation;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Get.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Get.Validation;
using MyFinanceTracker.UseCases.Transaction.Update;
using MyFinanceTracker.UseCases.Transaction.Update.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Update.Validation;

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
            .AddTransactionGet()
            .AddTransactionUpdate();
    }

    private static IServiceCollection AddTransactionCreate(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<CreateTransactionsRequest, CreateTransactionsResponse>,
                CreateTransactionsLoggingBehavior>()
            .AddScoped<IValidator<CreateTransactionsRequest>, CreateTransactionsRequestValidator>()
            .AddValidationBehavior<CreateTransactionsRequest, CreateTransactionsResponse>(
                errors => new CreateTransactionsResponse.ValidationError(errors));
    }

    private static IServiceCollection AddTransactionDelete(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>,
                DeleteTransactionsLoggingBehavior>()
            .AddScoped<IValidator<DeleteTransactionsRequest>, DeleteTransactionsRequestValidator>()
            .AddValidationBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>(
                errors => new DeleteTransactionsResponse.ValidationError(errors));
    }

    private static IServiceCollection AddTransactionGet(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<GetTransactionsRequest, GetTransactionsResponse>,
                GetTransactionsLoggingBehavior>()
            .AddScoped<IValidator<GetTransactionsRequest>, GetTransactionsRequestValidator>()
            .AddValidationBehavior<GetTransactionsRequest, GetTransactionsResponse>(
                errors => new GetTransactionsResponse.ValidationError(errors));
    }

    private static IServiceCollection AddTransactionUpdate(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<UpdateTransactionRequest, UpdateTransactionResponse>,
                UpdateTransactionLoggingBehavior>()
            .AddScoped<IValidator<UpdateTransactionRequest>, UpdateTransactionRequestValidator>()
            .AddValidationBehavior<UpdateTransactionRequest, UpdateTransactionResponse>(
                errors => new UpdateTransactionResponse.ValidationError(errors));
    }

    private static IServiceCollection AddCategoriesUseCases(this IServiceCollection services)
    {
        return services
            .AddListCategories()
            .AddCreateCategory()
            .AddUpdateCategory();
    }

    private static IServiceCollection AddListCategories(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<ListCategoriesRequest, ListCategoriesResponse>,
                ListCategoriesLoggingBehavior>();
    }

    private static IServiceCollection AddCreateCategory(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<CreateCategoryRequest, CreateCategoryResponse>,
                CreateCategoryLoggingBehavior>()
            .AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryRequestValidator>()
            .AddValidationBehavior<CreateCategoryRequest, CreateCategoryResponse>(
                errors => new CreateCategoryResponse.ValidationError(errors));
    }

    private static IServiceCollection AddUpdateCategory(this IServiceCollection services)
    {
        return services
            .AddTransient<
                IPipelineBehavior<UpdateCategoryRequest, UpdateCategoryResponse>,
                UpdateCategoryLoggingBehavior>()
            .AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryRequestValidator>()
            .AddValidationBehavior<UpdateCategoryRequest, UpdateCategoryResponse>(
                errors => new UpdateCategoryResponse.ValidationError(errors));
    }

    private static IServiceCollection AddValidationBehavior<TRequest, TResponse>(
        this IServiceCollection services,
        Func<IReadOnlyList<ValidationErrorItem>, TResponse> errorFactory)
        where TRequest : notnull
        where TResponse : UseCaseResponse
    {
        return services.AddTransient<IPipelineBehavior<TRequest, TResponse>>(sp =>
            new ValidationBehavior<TRequest, TResponse>(
                sp.GetRequiredService<IValidator<TRequest>>(),
                errorFactory));
    }
}
