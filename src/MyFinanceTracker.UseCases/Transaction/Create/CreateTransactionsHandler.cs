using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(CreateTransactionsRequest request, CancellationToken ct)
    {
        if (request.Items.Any(i => string.IsNullOrWhiteSpace(i.CategoryId)))
        {
            return new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryRequired)
            ]);
        }

        var distinctIds = request.Items
            .Select(i => i.CategoryId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var foundCategories = await categoryRepository.GetByIds(distinctIds, ct);
        var categoriesById = foundCategories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        var transactions = new List<Domain.Entities.Transaction>(request.Items.Count);
        foreach (var item in request.Items)
        {
            if (!categoriesById.TryGetValue(item.CategoryId.Trim(), out var category))
            {
                return new CreateTransactionsResponse.ValidationError([
                    new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, item.CategoryId)
                ]);
            }

            if (category.IsArchived)
            {
                return new CreateTransactionsResponse.ValidationError([
                    new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryArchived, item.CategoryId)
                ]);
            }

            transactions.Add(new Domain.Entities.Transaction(
                id: Guid.NewGuid(),
                type: item.TransactionType,
                category: category,
                amount: item.Amount,
                date: item.Date,
                note: item.Note
            ));
        }

        await transactionRepository.AddRange(transactions, ct);

        return new CreateTransactionsResponse.Success(transactions);
    }
}
