using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ILogger<CreateTransactionHandler> logger)
    : IRequestHandler<CreateTransactionRequest, CreateTransactionResult>
{
    public async Task<CreateTransactionResult> Handle(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Attempting to create {Type} transaction for category '{Alias}'. Amounts count: {Count}",
                request.TransactionType, request.CategoryAlias, request.Amounts.Length);

            var category = await categoryRepository.GetByAlias(request.CategoryAlias, cancellationToken);

            if (category is null)
            {
                logger.LogWarning("CreateTransaction failed: Category with alias '{Alias}' was not found", request.CategoryAlias);

                return new CreateTransactionResult.Failure($"Unknown category: {request.CategoryAlias}");
            }

            var transactions = request.Amounts.Select(amount =>
                new Domain.Entities.Transaction(
                    Guid.NewGuid(),
                    request.TransactionType,
                    category,
                    ApplyBusinessRulesToAmount(amount, request.TransactionType),
                    request.Date,
                    request.Note
                )).ToList();

            await transactionRepository.AddRange(transactions, cancellationToken);

            logger.LogInformation("Successfully recorded {Count} transactions into '{CategoryName}'",
                transactions.Count, category.Name);

            return new CreateTransactionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical failure during transaction creation for category '{Alias}'", request.CategoryAlias);

            return new CreateTransactionResult.Failure("A system error occurred while saving data to the registry.");
        }
    }

    private static decimal ApplyBusinessRulesToAmount(decimal amount, TransactionType type)
    {
        return type switch
        {
            TransactionType.Expense => -Math.Abs(amount),
            TransactionType.Income => Math.Abs(amount),
            _ => amount
        };
    }
}