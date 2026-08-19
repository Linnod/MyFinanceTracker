using System.Diagnostics;
using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Common;

namespace MyFinanceTracker.UseCases.Transaction.Create;

internal sealed class CreateTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    TimeProvider timeProvider)
    : IRequestHandler<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(CreateTransactionsRequest request, CancellationToken ct)
    {
        var allCategories = await categoryRepository.GetAll(ct);
        var lookup = new CategoryLookup(allCategories);

        var transactions = new List<Domain.Entities.Transaction>(request.Items.Count);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
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
                date: item.Date ?? today,
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
        var alias = string.IsNullOrWhiteSpace(item.CategoryAlias) && item.TransactionType == TransactionType.Income
            ? FinancialRules.DefaultIncomeCategoryAlias
            : item.CategoryAlias;

        if (string.IsNullOrWhiteSpace(alias))
        {
            return (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryRequired)
            ]));
        }

        return lookup.Resolve(alias) switch
        {
            CategoryResolution.Found found => (found.Category, null),
            CategoryResolution.NotFound notFound => (null, new CreateTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, notFound.Suggestion)
            ])),
            _ => throw new UnreachableException()
        };
    }
}