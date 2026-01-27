using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

internal sealed class DeleteTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ILogger<DeleteTransactionsHandler> logger)
    : IRequestHandler<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        CancellationToken ct)
    {
        logger.LogInformation("--> Handle");

        var response = await HandleInternal(request, ct);

        logger.LogInformation("<-- Handle");

        return response;
    }

    private async Task<DeleteTransactionsResponse> HandleInternal(DeleteTransactionsRequest request, CancellationToken ct)
    {
        var category = await categoryRepository.GetByAlias(request.CategoryAlias!, ct);
        if (category is null)
        {
            logger.LogWarning("🔍 Category '{Alias}' not found", request.CategoryAlias);

            return new DeleteTransactionsResponse.ValidationError($"Unknown category: {request.CategoryAlias}");
        }

        var finalDate = request.Date!.Value;
        try
        {
            await transactionRepository.DeleteRange(category, finalDate, ct);

            logger.LogInformation("✅ Successfully cleared entries for {CatName} ({Date})",
                category.Name, finalDate);

            return new DeleteTransactionsResponse.Success(category.Name, finalDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to delete transactions for {Alias}", request.CategoryAlias);

            return new DeleteTransactionsResponse.Failure("Failed to clear transactions. System error.");
        }
    }
}