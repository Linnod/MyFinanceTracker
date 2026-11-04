using MediatR;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Common;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(CreateTransactionsRequest request, CancellationToken ct)
    {
        var allCategories = await categoryRepository.GetAll(ct);
        var lookup = new CategoryLookup(allCategories);

        var transactions = new List<Domain.Entities.Transaction>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var (category, error) = ResolveCategory(item, lookup);
            if (error != null)
            {
                return error;
            }

            transactions.Add(new Domain.Entities.Transaction(
                id: Guid.NewGuid(),
                type: item.TransactionType,
                category: category!,
                amount: item.Amount,
                date: item.Date,
                note: item.Note
            ));
        }

        await transactionRepository.AddRange(transactions, ct);

        return new CreateTransactionsResponse.Success(transactions);
    }

    private static (Domain.Entities.Category? Category, CreateTransactionsResponse? Error) ResolveCategory(
        CreateTransactionItem item,
        CategoryLookup lookup)
    {
        if (string.IsNullOrWhiteSpace(item.CategoryAlias))
        {
            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryRequired)
            ]));
        }

        var category = lookup.Resolve(item.CategoryAlias);
        if (category is null)
        {
            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, item.CategoryAlias)
            ]));
        }

        return (category, null);
    }
}
