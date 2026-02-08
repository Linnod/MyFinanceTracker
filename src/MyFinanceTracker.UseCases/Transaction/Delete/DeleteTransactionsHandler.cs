using MediatR;
using MyFinanceTracker.Common.Utilities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

internal sealed class DeleteTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByAlias(request.CategoryAlias!, ct);
        if (category is null)
        {
            var allAliases = (await categoryRepository.GetAll(ct)).SelectMany(c => c.Aliases);
            var suggestion = FuzzyMatcher.GetClosest(request.CategoryAlias!, allAliases);

            return DeleteTransactionsResponse.ValidationError.FromSingle(
                nameof(request.CategoryAlias),
                $"Unknown category: '{request.CategoryAlias!}'",
                suggestion);
        }

        await transactionRepository.DeleteRange(category, request.Date!.Value, ct);

        return new DeleteTransactionsResponse.Success(category.Name, request.Date.Value);
    }
}