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
        var transactions = new List<Domain.Entities.Transaction>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var (category, error) = await ResolveCategory(item, ct);
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

    private async Task<(Domain.Entities.Category? Category, CreateTransactionsResponse? Error)> ResolveCategory(
        CreateTransactionItem item,
        CancellationToken ct)
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

        var category = await categoryRepository.GetByAlias(alias, ct);
        if (category is null)
        {
            var allAliases = (await categoryRepository.GetAll(ct)).SelectMany(c => c.Aliases);
            var suggestion = FuzzyMatcher.GetClosest(alias, allAliases);

            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, suggestion)
            ]));
        }

        return (category, null);
    }
}