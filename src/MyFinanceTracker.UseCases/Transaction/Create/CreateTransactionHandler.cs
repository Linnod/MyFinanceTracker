using MediatR;
using MyFinanceTracker.Common.Utilities;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateTransactionRequest, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(CreateTransactionRequest request, CancellationToken ct)
    {
        var (category, error) = await GetCategory(request, ct);
        if (error != null)
        {
            return error;
        }

        var finalDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var transactions = request.Amounts.Select(amount =>
            new Domain.Entities.Transaction(
                Guid.NewGuid(),
                request.TransactionType,
                category!,
                ApplyBusinessRulesToAmount(amount, request.TransactionType),
                finalDate,
                request.Note
            )).ToList();

        await transactionRepository.AddRange(transactions, ct);

        return new CreateTransactionResponse.Success(
            category!.Name,
            request.Amounts,
            finalDate,
            request.Note);
    }

    private async Task<(Category? Category, CreateTransactionResponse.ValidationError? Error)> GetCategory(
        CreateTransactionRequest request, CancellationToken ct)
    {
        var alias = string.IsNullOrWhiteSpace(request.CategoryAlias) && request.TransactionType == TransactionType.Income
            ? FinancialRules.DefaultIncomeCategoryAlias
            : request.CategoryAlias;
        if (string.IsNullOrWhiteSpace(alias))
        {
            return (null, CreateTransactionResponse.ValidationError.FromSingle(
                nameof(request.CategoryAlias),
                $"Category is required for {request.TransactionType.ToString().ToLower()}"));
        }

        var category = await categoryRepository.GetByAlias(alias, ct);
        if (category is null)
        {
            var allAliases = await categoryRepository.GetAllAliases(ct);
            var suggestion = FuzzyMatcher.GetClosest(alias, allAliases);

            return (null, CreateTransactionResponse.ValidationError.FromSingle(
                nameof(request.CategoryAlias),
                $"Unknown category: '{alias}'",
                suggestion));
        }

        return (category, null);
    }

    private static decimal ApplyBusinessRulesToAmount(decimal amount, TransactionType type)
    {
        var absAmount = Math.Abs(amount);
        return type == TransactionType.Expense ? -absAmount : absAmount;
    }
}