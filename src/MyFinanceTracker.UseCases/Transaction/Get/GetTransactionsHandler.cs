using MediatR;
using MyFinanceTracker.Common.Utilities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Get;

internal sealed class GetTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionsRequest, GetTransactionsResponse>
{
    public async Task<GetTransactionsResponse> Handle(
        GetTransactionsRequest request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByAlias(request.CategoryAlias!, ct);
        if (category is null)
        {
            var allAliases = (await categoryRepository.GetAll(ct)).SelectMany(c => c.Aliases);
            var suggestion = FuzzyMatcher.GetClosest(request.CategoryAlias!, allAliases);

            return new GetTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, suggestion)
            ]);
        }

        var transactions = await transactionRepository.Get(
            category,
            request.Date!.Value,
            ct);

        return new GetTransactionsResponse.Success(
            category.Name,
            request.Date.Value,
            transactions);
    }
}