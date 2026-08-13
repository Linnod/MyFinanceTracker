using MediatR;
using MyFinanceTracker.Common.Utilities;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(CreateTransactionsRequest request, CancellationToken ct)
    {
        var allCategories = await categoryRepository.GetAll(ct);
        var categoriesByAlias = allCategories
            .SelectMany(c => c.Aliases.Select(alias => new { Alias = alias, Category = c }))
            .ToDictionary(x => x.Alias, x => x.Category, StringComparer.OrdinalIgnoreCase);
        var transactions = new List<Domain.Entities.Transaction>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var (category, error) = ResolveCategory(item, categoriesByAlias);
            if (error != null)
            {
                return error;
            }

            transactions.Add(new Domain.Entities.Transaction(
                id: Guid.NewGuid(),
                type: item.TransactionType,
                category: category!,
                amount: item.Amount,
                date: item.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                note: item.Note
            ));
        }

        await transactionRepository.AddRange(transactions, ct);

        return new CreateTransactionsResponse.Success(transactions);
    }

    private static (Domain.Entities.Category? Category, CreateTransactionsResponse? Error) ResolveCategory(
        CreateTransactionItem item,
        Dictionary<string, Domain.Entities.Category> categoriesByAlias)
    {
        var alias = string.IsNullOrWhiteSpace(item.CategoryAlias) && item.TransactionType == TransactionType.Income
            ? FinancialRules.DefaultIncomeCategoryAlias
            : item.CategoryAlias;

        if (string.IsNullOrWhiteSpace(alias))
        {
            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryRequired)
            ]));
        }

        if (!categoriesByAlias.TryGetValue(alias, out var category))
        {
            var suggestion = FuzzyMatcher.GetClosest(alias, categoriesByAlias.Keys);

            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, suggestion)
            ]));
        }

        return (category, null);
    }
}