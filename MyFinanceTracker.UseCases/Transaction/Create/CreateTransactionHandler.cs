using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateTransactionRequest, CreateTransactionResult>
{
    public async Task<CreateTransactionResult> Handle(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByAlias(request.CategoryAlias, cancellationToken);
            if (category is null)
            {
                return CreateTransactionResult.Failure($"Unknown category: {request.CategoryAlias}");
            }

            var transactions = request.Amounts.Select(amount =>
                new Transaction(
                    category,
                    ApplyBusinessRulesToAmount(amount, request.Type),
                    request.Date,
                    request.Note
                )).ToList();

            await transactionRepository.AddRange(transactions, cancellationToken);

            return CreateTransactionResult.Success();
        }
        catch (Exception ex)
        {
            return CreateTransactionResult.Failure(ex.Message);
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