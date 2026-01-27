using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ILogger<CreateTransactionHandler> logger)
    : IRequestHandler<CreateTransactionRequest, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(CreateTransactionRequest request, CancellationToken ct)
    {
        var finalDate = request.Date ?? DateOnly.FromDateTime(DateTime.Now);

        var (categoryAlias, categoryError) = ResolveCategoryAlias(request);
        if (categoryError != null)
        {
             return categoryError;
        }

        var category = await categoryRepository.GetByAlias(categoryAlias!, ct);
        if (category is null)
        {
            logger.LogWarning("🔍 Category '{Alias}' not found", categoryAlias);
            
            return new CreateTransactionResponse.ValidationError($"Unknown category: {categoryAlias}");
        }

        try
        {
            var transactions = request.Amounts.Select(amount =>
                new Domain.Entities.Transaction(
                    Guid.NewGuid(),
                    request.TransactionType,
                    category,
                    ApplyBusinessRulesToAmount(amount, request.TransactionType),
                    finalDate,
                    request.Note
                )).ToList();

            await transactionRepository.AddRange(transactions, ct);

            logger.LogInformation("✅ Successfully recorded {Count} transactions for {CatName}",
                transactions.Count, category.Name);

            return new CreateTransactionResponse.Success(
                CategoryName: category.Name,
                Amounts: request.Amounts,
                Date: finalDate,
                Note: request.Note
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to create transactions for category '{Alias}'", categoryAlias);

            return new CreateTransactionResponse.Failure("System error during transaction creation.");
        }
    }

    private static (string? Alias, CreateTransactionResponse.ValidationError? Error) ResolveCategoryAlias(CreateTransactionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CategoryAlias))
            return (request.CategoryAlias, null);

        if (request.TransactionType == TransactionType.Income)
            return (FinancialRules.DefaultIncomeCategoryAlias, null);

        var typeName = request.TransactionType.ToString().ToLower();
        return (null, new CreateTransactionResponse.ValidationError($"Category is required for {typeName} transactions."));
    }

    private static decimal ApplyBusinessRulesToAmount(decimal amount, TransactionType type)
    {
        var absAmount = Math.Abs(amount);
        return type == TransactionType.Expense ? -absAmount : absAmount;
    }
}