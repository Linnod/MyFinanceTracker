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
                request.Type, request.CategoryAlias, request.Amounts.Length);

            var category = await categoryRepository.GetByAlias(request.CategoryAlias, cancellationToken);
            
            if (category is null)
            {
                logger.LogWarning("CreateTransaction failed: Category with alias '{Alias}' was not found", request.CategoryAlias);
                
                return CreateTransactionResult.Failure($"Unknown category: {request.CategoryAlias}");
            }

            var transactions = request.Amounts.Select(amount =>
                new Domain.Entities.Transaction(
                    category,
                    ApplyBusinessRulesToAmount(amount, request.Type),
                    request.Date,
                    request.Note
                )).ToList();

            await transactionRepository.AddRange(transactions, cancellationToken);

            logger.LogInformation("Successfully recorded {Count} transactions into '{CategoryName}'", 
                transactions.Count, category.Name);

            return CreateTransactionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical failure during transaction creation for category '{Alias}'", request.CategoryAlias);
            
            return CreateTransactionResult.Failure("A system error occurred while saving data to the registry.");
        }
    }

    private static decimal ApplyBusinessRulesToAmount(decimal amount, FinancialOperationType type)
    {
        return type switch
        {
            FinancialOperationType.Expense => -Math.Abs(amount),
            FinancialOperationType.Income => Math.Abs(amount),
            FinancialOperationType.Return => Math.Abs(amount),
            FinancialOperationType.Adjustment => amount,
            _ => amount
        };
    }
}